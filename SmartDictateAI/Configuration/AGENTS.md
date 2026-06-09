# SmartDictate Configuration DOX

## Purpose
Houses the user configurations mapping class (`AppSettings.cs`) matching the schema of `appsettings.json`.

## Ownership
SmartDictate Configuration / Data transfer layer.

## Local Contracts
- Schema Mapping: Properties must correspond directly to settings in `appsettings.json`.
- Transactional Cloning: AppSettings provides cloning/copy mechanisms to support Cancel/OK dialog transactions.

## Work Guidance
- Any changes to AppSettings structure must be reflected in `appsettings.json` template config.

## Verification
- Run xUnit tests in `SmartDictateAI.Tests` focusing on `AppSettingsTests.cs`.

## Child DOX Index
This directory has no nested subdirectories with a DOX contract.
