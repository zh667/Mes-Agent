# TypeScript Rules

Use these rules for React frontend code, generated clients, scripts, and any
TypeScript shared tooling.

## Compiler Settings

Prefer strict TypeScript:

```json
{
  "compilerOptions": {
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true,
    "noImplicitOverride": true
  }
}
```

## Types

- Do not use `any` unless isolating an untyped third-party boundary. Prefer
  `unknown` plus validation.
- Do not hand-write backend response types in frontend code. Import generated
  types from the API client or shared contract layer.
- Keep domain names aligned with backend contracts: `workOrderId`,
  `batchNumber`, `operationId`, `equipmentId`, `inspectionResult`.
- Model UI-only state separately from API DTOs.
- Use discriminated unions for stateful workflows such as loading/error/success
  or equipment status views.

## Data Fetching

- Use the generated API client for backend calls.
- Put hand-authored fetch wrappers around generated clients only for cross-cutting
  concerns such as auth headers, retry policy, correlation IDs, and error
  normalization.
- React projects may use TanStack Query over the typed API client.
- Do not parse API responses ad hoc in components.

## Formatting And Linting

- Prefer ESLint flat config with `typescript-eslint`.
- Prefer Prettier for formatting.
- Keep imports ordered by the project toolchain.
- Do not commit generated API client changes without the contract change that
  produced them.

## Acceptance Checks

Run the matching commands when available:

```powershell
pnpm lint
pnpm typecheck
pnpm test
pnpm build
```
