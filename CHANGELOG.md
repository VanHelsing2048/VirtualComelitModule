# Changelog

## 1.3.0

- Persist virtual module output state and parameter memory across add-on restarts.
- Add module type metadata to configuration and persisted state for future module kinds.

## 1.2.0

- Add simplified add-on configuration for virtual module addresses.
- Keep advanced per-module configuration compatible with previous releases.

## 1.1.0

- Add virtual memory parameter handling for Comelit read/write commands.
- Respond to SimpleProg-style memory commands `77`, `AA`, `78`, and `AB`.
- Seed module type and firmware/version cells for virtual I/O module discovery.

## 1.0.0

- Initial Home Assistant add-on repository.
- Add virtual Comelit I/O 8-output bus responder.
- Support configurable module addresses through add-on options.
