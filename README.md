### PostgreSQL Database Library

database library
## SonarCloud Analysis

This project uses SonarCloud for code quality analysis.

**Important:** To avoid conflicts between CI-based analysis and SonarCloud Automatic Analysis, ensure that **Automatic Analysis** is **disabled** in the SonarCloud project settings:
1. Go to your project in SonarCloud.
2. Navigate to **Administration** > **Analysis Method**.
3. Toggle **Automatic Analysis** to **OFF**.

The CI analysis is performed via GitHub Actions using the `.github/workflows/sonar.yml` workflow and the `sonar.cake` script.
