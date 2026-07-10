## Summary

- Describe the change and its operational impact.

## Verification

- [ ] Relevant backend tests pass
- [ ] `pnpm lint` passes for frontend changes
- [ ] `pnpm typecheck`, `pnpm test`, and `pnpm build` pass for frontend changes

## Localization

- [ ] New or changed user-visible copy is present in both `web/messages/en-US.json` and `web/messages/zh-CN.json`
- [ ] Accessible labels, placeholders, errors, empty states, and status text use message keys
- [ ] `pnpm check:i18n` passes, or each intentional literal has an `i18n-ignore: <reason>` comment
- [ ] Locale switching preserves the current route and workflow

## Security And Data

- [ ] No secrets, production credentials, customer data, or private manufacturing data are included
- [ ] Tenant and authorization boundaries were considered where applicable
