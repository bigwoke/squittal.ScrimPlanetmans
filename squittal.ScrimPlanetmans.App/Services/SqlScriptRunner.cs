using System;
using System.IO;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;

namespace squittal.ScrimPlanetmans.Services
{
    public class SqlScriptRunner : ISqlScriptRunner
    {
        private readonly string _sqlDirectory = Path.Combine("Data", "SQL");
        private readonly string _basePath;
        private readonly string _scriptDirectory;
        private readonly string _adhocScriptDirectory;

        private readonly ILogger<SqlScriptRunner> _logger;
        private readonly IConfiguration _config;

        public SqlScriptRunner(ILogger<SqlScriptRunner> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            _basePath = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
            _scriptDirectory = Path.Combine(_basePath, _sqlDirectory);

            _adhocScriptDirectory = Path.GetFullPath(Path.Combine(_basePath, "..", "..", "..", "..", "sql_adhoc"));
        }

        public void RunSqlScript(string fileName, bool minimalLogging = false)
        {
            var scriptPath = Path.Combine(_scriptDirectory, fileName);
            
            try
            {
                var scriptFileInfo = new FileInfo(scriptPath);

                string scriptText = scriptFileInfo.OpenText().ReadToEnd();

                RunSqlCommand(scriptText);

                if (!minimalLogging)
                {
                    _logger.LogInformation($"Successfully ran sql script at {scriptPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error running sql script {scriptPath}: {ex}");
            }
        }

        public bool TryRunAdHocSqlScript(string fileName, out string info, bool minimalLogging = false)
        {
            var scriptPath = Path.Combine(_adhocScriptDirectory, fileName);

            try
            {
                var scriptFileInfo = new FileInfo(scriptPath);

                string scriptText = scriptFileInfo.OpenText().ReadToEnd();

                RunSqlCommand(scriptText);

                info = $"Successfully ran sql script at {scriptPath}";

                if (!minimalLogging)
                {
                    _logger.LogInformation(info);
                }

                return true;
            }
            catch (Exception ex)
            {
                info = $"Error running sql script {scriptPath}: {ex}";

                _logger.LogError(info);

                return false;
            }
        }

        public void RunSqlDirectoryScripts(string directoryName)
        {
            var directoryPath = Path.Combine(_scriptDirectory, directoryName);

            try
            {
                var files = Directory.GetFiles(directoryPath)
                    .Where(f => f.EndsWith(".sql"))
                    .Order();

                foreach (var file in files)
                {
                    RunSqlScript(file, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error running SQL scripts in directory {directoryName}: {ex}");
            }
        }

        private void RunSqlCommand(string text)
        {
            using (SqlConnection connection = new(_config.GetConnectionString("PlanetmansDbContext")))
            {
                string[] commands = Regex.Split(text, @"^\s*GO\s*$",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);

                connection.Open();

                foreach (string commandText in commands)
                {
                    if (string.IsNullOrWhiteSpace(commandText)) continue;

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
