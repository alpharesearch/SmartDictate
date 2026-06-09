# SmartDictate Tests DOX

## Purpose
Unit test suite for checking local service interfaces, configuration mappings, and transcription orchestrations in isolation.

## Ownership
SmartDictate Quality Assurance.

## Local Contracts
- Framework: xUnit is the testing framework.
- Platform Target: Must target `net9.0-windows` for complete compatibility with the main project.
- Mocks: Avoid using third-party Mocking engines (like Moq, NSubstitute). Instead, use lightweight, self-contained mock implementations of service interfaces (found inside the test project).
- Test Naming Pattern: Test classes should append `Tests` to the name of the class being tested. Test methods should follow `MethodName_StateUnderTest_ExpectedBehavior`.

## Work Guidance
- Tests are executed via standard test runners or the command line.

## Verification
- To run tests using .NET CLI, execute:
  ```bash
  dotnet test
  ```

## Child DOX Index
This directory has no nested subdirectories with a DOX contract.
