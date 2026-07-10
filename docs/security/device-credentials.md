# Device Credential Handling

- API responses never return MQTT passwords, OPC UA private keys, or Modbus credentials.
- Stored secrets are protected with ASP.NET Core Data Protection. Production requires a persistent key ring outside the container layer.
- MQTT credentials require TLS in production. Anonymous broker access and topic spoofing are disabled.
- OPC UA secure mode requires an explicitly pinned certificate thumbprint. Insecure transport is a development-only opt-in.
- Modbus TCP is read-only in Phase 3 and validates address ranges before connecting.
- Rotate a credential by saving a replacement, validating connectivity, then revoking the old device-side credential. Keep the Data Protection key ring during rotation.

Use a plant secret manager for production values. Do not place credentials in Git, screenshots, audit payloads, or exported diagnostics.
