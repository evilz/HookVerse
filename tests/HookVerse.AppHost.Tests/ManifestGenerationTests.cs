using FluentAssertions;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace HookVerse.AppHost.Tests;

/// <summary>
/// Integration tests for Aspire manifest generation.
/// Verifies that Kubernetes YAML and Azure Bicep templates are generated correctly.
/// </summary>
public class ManifestGenerationTests
{
    private readonly string _repoRoot;
    private readonly string _appHostProject;
    private readonly string _k8sManifestPath;
    private readonly string _azureManifestPath;

    public ManifestGenerationTests()
    {
        _repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
        _appHostProject = Path.Combine(_repoRoot, "src", "HookVerse.AppHost", "HookVerse.AppHost.csproj");
        _k8sManifestPath = Path.Combine(_repoRoot, "aspire", "manifests", "kubernetes");
        _azureManifestPath = Path.Combine(_repoRoot, "aspire", "manifests", "azure");
    }

    [Fact]
    public async Task Kubernetes_Manifests_Are_Generated()
    {
        // Arrange: Clean up any existing manifests
        if (Directory.Exists(_k8sManifestPath))
        {
            Directory.Delete(_k8sManifestPath, recursive: true);
        }

        // Act: Generate Kubernetes manifests using dotnet publish
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{_appHostProject}\" /p:PublishProfile=kubernetes",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        process.Should().NotBeNull("dotnet publish process should start");

        var output = await process!.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert: Manifest generation should succeed
        process.ExitCode.Should().Be(0, 
            $"Manifest generation should succeed.\nOutput: {output}\nError: {error}");

        // Verify manifest directory was created
        Directory.Exists(_k8sManifestPath).Should().BeTrue(
            "Kubernetes manifest directory should be created");

        // Verify YAML files were generated
        var yamlFiles = Directory.GetFiles(_k8sManifestPath, "*.yaml", SearchOption.AllDirectories);
        yamlFiles.Should().NotBeEmpty("At least one YAML file should be generated");
        
        // Verify key manifests exist
        yamlFiles.Should().Contain(f => f.Contains("api"), "API deployment manifest should exist");
        yamlFiles.Should().Contain(f => f.Contains("worker"), "Worker deployment manifest should exist");
    }

    [Fact]
    public async Task Kubernetes_Manifests_Are_Valid()
    {
        // Arrange: Ensure manifests exist (generate if needed)
        if (!Directory.Exists(_k8sManifestPath) || !Directory.GetFiles(_k8sManifestPath, "*.yaml", SearchOption.AllDirectories).Any())
        {
            await Kubernetes_Manifests_Are_Generated();
        }

        // Get all YAML files
        var yamlFiles = Directory.GetFiles(_k8sManifestPath, "*.yaml", SearchOption.AllDirectories);
        yamlFiles.Should().NotBeEmpty("YAML files should exist before validation");

        // Act & Assert: Validate each YAML file using kubectl
        foreach (var yamlFile in yamlFiles)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "kubectl",
                Arguments = $"apply --dry-run=client -f \"{yamlFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                // kubectl not installed, skip validation
                return;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // If kubectl is not available, skip this test
            if (error.Contains("not found") || error.Contains("not recognized"))
            {
                return;
            }

            process.ExitCode.Should().Be(0, 
                $"Kubernetes manifest {Path.GetFileName(yamlFile)} should be valid.\nOutput: {output}\nError: {error}");
        }
    }

    [Fact]
    public async Task Azure_Bicep_Is_Generated()
    {
        // Arrange: Clean up any existing manifests
        if (Directory.Exists(_azureManifestPath))
        {
            // Keep README.md if it exists
            var readmePath = Path.Combine(_azureManifestPath, "README.md");
            var hasReadme = File.Exists(readmePath);
            string? readmeContent = null;
            if (hasReadme)
            {
                readmeContent = await File.ReadAllTextAsync(readmePath);
            }

            Directory.Delete(_azureManifestPath, recursive: true);
            Directory.CreateDirectory(_azureManifestPath);

            if (hasReadme && readmeContent != null)
            {
                await File.WriteAllTextAsync(readmePath, readmeContent);
            }
        }

        // Act: Generate Azure Bicep templates using dotnet publish
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{_appHostProject}\" /p:PublishProfile=azure",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        process.Should().NotBeNull("dotnet publish process should start");

        var output = await process!.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert: Manifest generation should succeed
        process.ExitCode.Should().Be(0, 
            $"Bicep generation should succeed.\nOutput: {output}\nError: {error}");

        // Verify manifest directory was created
        Directory.Exists(_azureManifestPath).Should().BeTrue(
            "Azure manifest directory should be created");

        // Verify Bicep files were generated
        var bicepFiles = Directory.GetFiles(_azureManifestPath, "*.bicep", SearchOption.AllDirectories);
        bicepFiles.Should().NotBeEmpty("At least one Bicep file should be generated");
    }

    [Fact]
    public async Task Azure_Bicep_Is_Valid()
    {
        // Arrange: Ensure manifests exist (generate if needed)
        if (!Directory.Exists(_azureManifestPath) || !Directory.GetFiles(_azureManifestPath, "*.bicep", SearchOption.AllDirectories).Any())
        {
            await Azure_Bicep_Is_Generated();
        }

        // Get all Bicep files
        var bicepFiles = Directory.GetFiles(_azureManifestPath, "*.bicep", SearchOption.AllDirectories);
        bicepFiles.Should().NotBeEmpty("Bicep files should exist before validation");

        // Act & Assert: Validate each Bicep file using az bicep build
        foreach (var bicepFile in bicepFiles)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "az",
                Arguments = $"bicep build --file \"{bicepFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                // az CLI not installed, skip validation
                return;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // If az CLI is not available, skip this test
            if (error.Contains("not found") || error.Contains("not recognized"))
            {
                return;
            }

            process.ExitCode.Should().Be(0, 
                $"Azure Bicep file {Path.GetFileName(bicepFile)} should be valid.\nOutput: {output}\nError: {error}");
        }
    }

    [Fact]
    public async Task Manifest_Generation_Performance()
    {
        // Arrange: Target is < 30 seconds per Success Criteria SC-004
        var targetSeconds = 30;

        // Act: Measure Kubernetes manifest generation time
        var stopwatch = Stopwatch.StartNew();

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{_appHostProject}\" /p:PublishProfile=kubernetes",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        process.Should().NotBeNull("dotnet publish process should start");

        await process!.WaitForExitAsync();
        stopwatch.Stop();

        // Assert: Generation should complete within target time
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(targetSeconds,
            $"Kubernetes manifest generation should complete in less than {targetSeconds} seconds (SC-004)");

        // Also measure Azure Bicep generation
        stopwatch.Restart();

        startInfo.Arguments = $"publish \"{_appHostProject}\" /p:PublishProfile=azure";
        using var azureProcess = Process.Start(startInfo);
        azureProcess.Should().NotBeNull("dotnet publish process should start");

        await azureProcess!.WaitForExitAsync();
        stopwatch.Stop();

        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(targetSeconds,
            $"Azure Bicep generation should complete in less than {targetSeconds} seconds (SC-004)");
    }

    [Fact]
    public async Task Generated_Manifests_Are_Deterministic()
    {
        // Arrange: Generate manifests twice and compare
        
        // First generation
        var k8sManifestPath1 = Path.Combine(_repoRoot, "aspire", "manifests", "kubernetes_run1");
        if (Directory.Exists(k8sManifestPath1))
        {
            Directory.Delete(k8sManifestPath1, recursive: true);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{_appHostProject}\" /p:PublishProfile=kubernetes /p:ManifestPublishPath={k8sManifestPath1}",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(startInfo))
        {
            await process!.WaitForExitAsync();
        }

        // Second generation
        var k8sManifestPath2 = Path.Combine(_repoRoot, "aspire", "manifests", "kubernetes_run2");
        if (Directory.Exists(k8sManifestPath2))
        {
            Directory.Delete(k8sManifestPath2, recursive: true);
        }

        startInfo.Arguments = $"publish \"{_appHostProject}\" /p:PublishProfile=kubernetes /p:ManifestPublishPath={k8sManifestPath2}";
        using (var process = Process.Start(startInfo))
        {
            await process!.WaitForExitAsync();
        }

        // Act: Compare generated files
        var files1 = Directory.GetFiles(k8sManifestPath1, "*.yaml", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(k8sManifestPath1, f))
            .OrderBy(f => f)
            .ToList();

        var files2 = Directory.GetFiles(k8sManifestPath2, "*.yaml", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(k8sManifestPath2, f))
            .OrderBy(f => f)
            .ToList();

        // Assert: Same files should be generated
        files1.Should().BeEquivalentTo(files2, 
            "Both runs should generate the same set of files");

        // Compare file contents (excluding timestamps and GUIDs)
        foreach (var file in files1)
        {
            var content1 = await File.ReadAllTextAsync(Path.Combine(k8sManifestPath1, file));
            var content2 = await File.ReadAllTextAsync(Path.Combine(k8sManifestPath2, file));

            // Normalize content by removing timestamps and potential GUIDs
            var normalizedContent1 = NormalizeManifestContent(content1);
            var normalizedContent2 = NormalizeManifestContent(content2);

            normalizedContent1.Should().Be(normalizedContent2,
                $"File {file} should have identical content across runs");
        }

        // Cleanup
        Directory.Delete(k8sManifestPath1, recursive: true);
        Directory.Delete(k8sManifestPath2, recursive: true);
    }

    private string NormalizeManifestContent(string content)
    {
        // Remove timestamps
        content = Regex.Replace(content, @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", "TIMESTAMP");
        
        // Remove GUIDs
        content = Regex.Replace(content, @"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", "GUID", RegexOptions.IgnoreCase);
        
        // Remove potential build numbers or versions with timestamps
        content = Regex.Replace(content, @":\d{8}\.\d+", ":VERSION");

        return content;
    }
}
