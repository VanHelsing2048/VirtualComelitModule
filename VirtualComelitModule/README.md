# Comelit Virtual Module

Virtual Comelit bus module add-on for Home Assistant.

The add-on connects to the Comelit bus endpoint and responds as one or more configurable 8-output virtual I/O modules.
It does not publish MQTT directly; MQTT discovery and entity handling are expected to be handled by `MQTT_NET_COMELIT`.
It also responds to Comelit memory read/write commands so tools such as SimpleProg can read and write module parameters on the virtual module.

## Configuration

```yaml
comelit-ip: "192.168.1.51"
comelit-port: 10011
comelit-password: ""
module-addresses:
  - 1
  - 2
  - 10
module-name-prefix: "Virtual I8"
initial-state: 0
```

Each module address must match the address used by the Comelit-side integration.

For advanced setups, the add-on also accepts the previous `modules` list with per-module `address`, `name`, and `initial-state`.
