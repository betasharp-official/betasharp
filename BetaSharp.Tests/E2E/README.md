# E2E Test Lanes

Initial end-to-end coverage includes:

- a fast headless join lane
- an in-process visual lane with debug UI forced on at launch

Run only the headless e2e tests:

`dotnet test --filter "Category=E2E&Mode=Headless"`

Run only the visual e2e tests:

`dotnet test --filter "Category=E2E&Mode=Visual"`

Run all e2e tests:

`dotnet test --filter "Category=E2E"`

Prerequisites:

- `b1.7.3.jar` must be available in the test output directory
- visual tests require an active desktop session (`DISPLAY` or `WAYLAND_DISPLAY` on Linux)
