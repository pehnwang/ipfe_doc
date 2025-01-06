using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace AdvancedVariableSystem.Storage
{
    #region 存储接口

    public interface IVariableStorage
    {
        void SaveVariables(Dictionary<string, VariableBase> variables);
        Dictionary<string, VariableBase> LoadVariables();
        void SaveSnapshot(string name, Dictionary<string, object> snapshot);
        Dictionary<string, object> LoadSnapshot(string name);
        void SaveHistory(string variableName, List<HistoryRecord> history);
        List<HistoryRecord> LoadHistory(string variableName, DateTime from, DateTime to);
    }

    #region JSON存储实现
    
    public class JsonVariableStorage : IVariableStorage
    {
        private readonly string _basePath;
        private readonly string _variablesPath;
        private readonly string _snapshotsPath;
        private readonly string _historyPath;

        public JsonVariableStorage(string basePath)
        {
            _basePath = basePath;
            _variablesPath = Path.Combine(basePath, "variables");
            _snapshotsPath = Path.Combine(basePath, "snapshots");
            _historyPath = Path.Combine(basePath, "history");

            Directory.CreateDirectory(_variablesPath);
            Directory.CreateDirectory(_snapshotsPath);
            Directory.CreateDirectory(_historyPath);
        }

        public void SaveVariables(Dictionary<string, VariableBase> variables)
        {
            var json = JsonConvert.SerializeObject(variables, Formatting.Indented);
            File.WriteAllText(Path.Combine(_variablesPath, "variables.json"), json);
        }

        public Dictionary<string, VariableBase> LoadVariables()
        {
            var path = Path.Combine(_variablesPath, "variables.json");
            if (!File.Exists(path))
                return new Dictionary<string, VariableBase>();

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Dictionary<string, VariableBase>>(json);
        }

        public void SaveSnapshot(string name, Dictionary<string, object> snapshot)
        {
            var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
            File.WriteAllText(Path.Combine(_snapshotsPath, $"{name}.json"), json);
        }

        public Dictionary<string, object> LoadSnapshot(string name)
        {
            var path = Path.Combine(_snapshotsPath, $"{name}.json");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Snapshot {name} not found");

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        public void SaveHistory(string variableName, List<HistoryRecord> history)
        {
            var json = JsonConvert.SerializeObject(history, Formatting.Indented);
            File.WriteAllText(Path.Combine(_historyPath, $"{variableName}_history.json"), json);
        }

        public List<HistoryRecord> LoadHistory(string variableName, DateTime from, DateTime to)
        {
            var path = Path.Combine(_historyPath, $"{variableName}_history.json");
            if (!File.Exists(path))
                return new List<HistoryRecord>();

            var json = File.ReadAllText(path);
            var history = JsonConvert.DeserializeObject<List<HistoryRecord>>(json);
            return history.FindAll(r => r.Timestamp >= from && r.Timestamp <= to);
        }
    }

    #endregion

    #region SQLite存储实现

    public class SqliteVariableStorage : IVariableStorage
    {
        private readonly string _connectionString;

        public SqliteVariableStorage(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            
            // 创建变量表
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Variables (
                        Name TEXT PRIMARY KEY,
                        Type TEXT NOT NULL,
                        Value TEXT,
                        Metadata TEXT
                    )";
                cmd.ExecuteNonQuery();
            }

            // 创建历史记录表
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS History (
                        VariableName TEXT,
                        Timestamp TEXT,
                        Value TEXT,
                        PRIMARY KEY (VariableName, Timestamp)
                    )";
                cmd.ExecuteNonQuery();
            }

            // 创建快照表
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Snapshots (
                        Name TEXT PRIMARY KEY,
                        Data TEXT,
                        Timestamp TEXT
                    )";
                cmd.ExecuteNonQuery();
            }
        }

        public void SaveVariables(Dictionary<string, VariableBase> variables)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            
            try
            {
                using var cmd = new SQLiteCommand(conn);
                cmd.CommandText = "INSERT OR REPLACE INTO Variables (Name, Type, Value, Metadata) VALUES (@Name, @Type, @Value, @Metadata)";
                
                foreach (var variable in variables)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@Name", variable.Key);
                    cmd.Parameters.AddWithValue("@Type", variable.Value.GetType().Name);
                    cmd.Parameters.AddWithValue("@Value", JsonConvert.Serialize