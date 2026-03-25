# Contributing to BetaSharp

You can contribute to BetaSharp with issues and PRs. Simply filing issues for problems you encounter is a great way to contribute. \
Contributing implementations is greatly appreciated.

## Getting Started

For detailed instructions on how to build and run the project, please refer to the Building section in the README file.

## Reporting Issues

We always welcome bug reports, feature requests and overall feedback. Here are a few tips on how you can make reporting your issue as effective as possible.

* **Finding existing issues:** Please avoid creating duplicate issues; it makes it harder to keep track of issues.
* **Choose the appropriate type:** Select the type of issue that fits the best, whether it is a bug report, feature request, or other.

## Creating Pull Requests

Follow the following tips to ensure creating an effective pull request.

Please do:

* **Do** follow the style enforced by `.editorconfig`, Your editor (Visual Studio, Rider, VS Code) should automatically respect these settings.
* **Do** follow well known C# conventions to write idiomatic C# contributions, for more visit Microsoft's reference for C#.
    - https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
    - https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
* **Do** include tests when adding new features. When fixing bugs, start with adding a test that highlights how the current behavior is broken.

Please do not:

* **Do not** surprise us with big pull requests. Instead, file an issue so we can agree on a direction before you invest a large amount of time.
* **Do not** add changes that conflict with existing vanilla behavior.

AI policy:

- **Allowed**: AI tools (ChatGPT, Copilot, etc.) are allowed to assist with coding and refactoring.
- **Quality Control**: Low-quality, "vibe-coded", or hallucinated code will be **rejected**.
- **Review**: You are responsible for every line of code you submit. Verify that AI-generated code is correct, follows project conventions, and compiles before submitting.

## Workflow

1. **Fork** the repository.
2. Create a **Feature Branch** for your changes (`git checkout -b feature/my-cool-feature`).
3. **Commit** your changes (`git commit -m "Add some cool feature"`).
4. **Push** to your branch (`git push origin feature/my-cool-feature`).
5. Open a **Pull Request**.
