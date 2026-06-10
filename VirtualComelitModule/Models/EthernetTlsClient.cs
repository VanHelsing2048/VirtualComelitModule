using System.Buffers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using ComelitVirtualModule.Models;

namespace VirtualComelitModule.Models;

public sealed class EthernetTlsClient : IAsyncDisposable, IDisposable
{
    private readonly string _ip;
    private readonly int _port;
    private readonly string _password;

    private TcpClient? _tcp;
    private SslStream? _ssl;
    private Task? _readerTask;
    private CancellationTokenSource? _cts;

    public bool IsConnected => _ssl is { IsAuthenticated: true } && _tcp is { Connected: true };

    /// <summary>
    /// Evento sollevato a ogni frame ricevuto (sempre 5 byte).
    /// La byte[] passata è una COPIA (puoi conservarla tranquillamente).
    /// </summary>
    public event Action<byte[]>? FrameReceived;

    public EthernetTlsClient(string ip, int port, string password)
    {
        _ip = ip;
        _port = port;
        _password = password;
    }

    /// <summary>
    /// Connette via TLS, esegue handshake JSON (newline-delimited):
    /// invia {"password": "..."} e si aspetta {"ok": true}.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (IsConnected)
            return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _tcp = new TcpClient { NoDelay = true };
        await _tcp.ConnectAsync(_ip, _port, _cts.Token).ConfigureAwait(false);

        _ssl = new SslStream(
            _tcp.GetStream(),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: ValidateServerCertificate, 
            userCertificateSelectionCallback: null);

        var sslOptions = new SslClientAuthenticationOptions
        {
            TargetHost = _ip, // se hai un SNI/hostname reale, mettilo qui
            EnabledSslProtocols = SslProtocols.Tls12,
            CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
            ClientCertificates = null
        };

        await _ssl.AuthenticateAsClientAsync(sslOptions, _cts.Token).ConfigureAwait(false);

        // --- Handshake JSON su stesso stream ---
        var authenticated = await DoJsonAuthAsync(_ssl, _password, _cts.Token).ConfigureAwait(false);
        if (!authenticated)
            throw new AuthenticationException("Autenticazione Home Server rifiutata.");

        // --- Avvio reader loop (binario) ---
        _readerTask = Task.Run(() => ReaderLoopAsync(_cts.Token));
    }

    /// <summary>
    /// Scrive un frame da 5 byte.
    /// </summary>
    public async Task WriteAsync(byte[] frame5, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(frame5);
        if (frame5.Length != 5) throw new ArgumentException("Il frame deve essere esattamente di 5 byte.", nameof(frame5));

        var ssl = _ssl ?? throw new InvalidOperationException("Non connesso. Chiama prima ConnectAsync().");

        await ssl.WriteAsync(frame5.AsMemory(0, 5), cancellationToken).ConfigureAwait(false);
        await ssl.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Chiude connessione e termina il reader.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_cts == null) return;

        try { _cts.Cancel(); } catch { /* ignore */ }

        if (_readerTask is not null)
        {
            try { await _readerTask.ConfigureAwait(false); } catch { /* ignore */ }
        }

        _ssl?.Dispose();
        _ssl = null;

        _tcp?.Close();
        _tcp?.Dispose();
        _tcp = null;

        _cts.Dispose();
        _cts = null;
    }

    // ======== Internals ========

    private async Task ReaderLoopAsync(CancellationToken ct)
    {
        var ssl = _ssl!;
        var buffer = ArrayPool<byte>.Shared.Rent(5);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await ReadExactAsync(ssl, buffer, 0, 5, ct).ConfigureAwait(false);

                // Copia difensiva da 5 byte per l'evento
                var frame = new byte[5];
                System.Buffer.BlockCopy(buffer, 0, frame, 0, 5);

                // Notifica
                FrameReceived?.Invoke(frame);
            }
        }
        catch (OperationCanceledException)
        {
            // Stop richiesto → esci pulito
        }
        catch (Exception)
        {
            // Qualsiasi altro errore chiude la connessione
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task ReadExactAsync(Stream s, byte[] buf, int offset, int count, CancellationToken ct)
    {
        int readTotal = 0;
        while (readTotal < count)
        {
            int n = await s.ReadAsync(buf.AsMemory(offset + readTotal, count - readTotal), ct).ConfigureAwait(false);
            if (n == 0) throw new IOException("Connessione chiusa dal peer.");
            readTotal += n;
        }
    }

    private static bool ValidateServerCertificate(object sender, X509Certificate? cert, X509Chain? chain, SslPolicyErrors errors)
    {
        //if (errors == SslPolicyErrors.None) return true;

        // ⚠️ In DEV potresti voler accettare self-signed:
        // return true;

        return true; // In produzione: valida davvero la chain
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private static string EscapeForJson(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");


    private static async Task<bool> DoJsonAuthAsync(Stream stream, string password, CancellationToken ct)
    {
        // Prepara il payload JSON esattamente come da esempio
        string json = $@"{{""request"":0,""password"":""{EscapeForJson(password)}""}}";
        byte[] writeBuf = Encoding.UTF8.GetBytes(json);

        // Buffer di lettura fisso (100 byte come nel tuo snippet)
        byte[] readBuf = new byte[100];

        // Avvia write e read in parallelo
        var writeTask = stream.WriteAsync(writeBuf.AsMemory(0, writeBuf.Length), ct).AsTask();
        var readTask = stream.ReadAsync(readBuf.AsMemory(0, readBuf.Length), ct).AsTask();

        // Timeout 2s (come Task.WaitAll(..., 2000))
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var timeoutTask = Task.Delay(2000, timeoutCts.Token);

        var all = Task.WhenAll(writeTask, readTask);
        var winner = await Task.WhenAny(all, timeoutTask).ConfigureAwait(false);

        if (winner == timeoutTask)
            throw new TimeoutException("Timeout autenticazione (2s).");

        // Stoppa il delay se ha vinto WhenAll
        timeoutCts.Cancel();

        // Propaga eventuali eccezioni di write/read
        await all.ConfigureAwait(false);

        // Byte realmente letti
        int readCount = readTask.Result;
        if (readCount <= 0)
            throw new IOException("Risposta di autenticazione vuota.");

        string res = Encoding.UTF8.GetString(readBuf, 0, readCount).Trim('\0', '\r', '\n', ' ');
        if (string.IsNullOrWhiteSpace(res))
            throw new IOException("Risposta di autenticazione vuota.");

        // Parsing "loginResult" (accetta numero o stringa)
        try
        {
            using var doc = JsonDocument.Parse(res);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            if (!doc.RootElement.TryGetProperty("loginResult", out var lr))
                return false;

            if (TryParseLoginResult(lr, out var result))
                return IsSuccessfulLogin(result);

            return false;
        }
        catch (JsonException)
        {
            // JSON non valido
            return false;
        }
    }

    private static bool TryParseLoginResult(JsonElement el, out HomeServerLoginResponse result)
    {
        // Possibili formati: numero (es. 1), stringa ("Correct")
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int n))
        {
            result = (HomeServerLoginResponse)n;
            return true;
        }
        if (el.ValueKind == JsonValueKind.String)
        {
            var s = el.GetString();
            if (Enum.TryParse<HomeServerLoginResponse>(s, ignoreCase: true, out var r))
            {
                result = r;
                return true;
            }
            // fallback per stringhe tipo "correct"
            if (string.Equals(s, "correct", StringComparison.OrdinalIgnoreCase))
            {
                result = HomeServerLoginResponse.Correct;
                return true;
            }
        }
        result = HomeServerLoginResponse.Error;
        return false;
    }

    private static bool IsSuccessfulLogin(HomeServerLoginResponse response)
        => response is HomeServerLoginResponse.Correct
            or HomeServerLoginResponse.NewPasswordSet
            or HomeServerLoginResponse.PasswordAlreadySet;

    private bool _disposed;
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(EthernetTlsClient));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisconnectAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await DisconnectAsync().ConfigureAwait(false);
    }

}
