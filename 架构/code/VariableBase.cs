using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AdvancedVariableSystem
{
    #region 基础模型
    
    // 变量基类
    public abstract class VariableBase
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public string Description { get; set; }
        public bool IsMarked { get; set; }
        public bool IsReadOnly { get; set; }
        public string Unit { get; set; }
        public DateTime LastModified { get; set; }
        public List<string> Aliases { get; set; } = new List<string>();
        public string Group { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public abstract object GetValue();
        public abstract void SetValue(object value);
        public abstract string GetValueString();
        public abstract bool TryParse(string input);
        
        // 变量历史记录
        protected VariableHistory History { get; } = new VariableHistory();
        
        protected void RecordHistory()
        {
            History.AddRecord(new HistoryRecord 
            { 
                Timestamp = DateTime.Now,
                Value = GetValue()
            });
        }
    }

    // 数值变量
    public class NumericVariable : VariableBase
    {
        private double _value;
        public double Value 
        { 
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    LastModified = DateTime.Now;
                    RecordHistory();
                }
            }
        }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public int Precision { get; set; }

        public override object GetValue() => Value;
        public override string GetValueString() => Value.ToString($"F{Precision}");
        public override void SetValue(object value) => Value = Convert.ToDouble(value);
        public override bool TryParse(string input)
        {
            if (double.TryParse(input, out double result) && result >= MinValue && result <= MaxValue)
            {
                Value = result;
                return true;
            }
            return false;
        }
    }

    // 字符串变量
    public class StringVariable : VariableBase
    {
        private string _value;
        public string Value 
        { 
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    LastModified = DateTime.Now;
                    RecordHistory();
                }
            }
        }
        public int MaxLength { get; set; }

        public override object GetValue() => Value;
        public override string GetValueString() => Value;
        public override void SetValue(object value) => Value = Convert.ToString(value);
        public override bool TryParse(string input)
        {
            if (input != null && (MaxLength == 0 || input.Length <= MaxLength))
            {
                Value = input;
                return true;
            }
            return false;
        }
    }

    // 时间变量
    public class DateTimeVariable : VariableBase
    {
        private DateTime _value;
        public DateTime Value 
        { 
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    LastModified = DateTime.Now;
                    RecordHistory();
                }
            }
        }
        public string Format { get; set; } = "yyyy-MM-dd HH:mm:ss";

        public override object GetValue() => Value;
        public override string GetValueString() => Value.ToString(Format);
        public override void SetValue(object value) => Value = Convert.ToDateTime(value);
        public override bool TryParse(string input)
        {
            if (DateTime.TryParse(input, out DateTime result))
            {
                Value = result;
                return true;
            }
            return false;
        }
    }

    #endregion

    #region 历史记录

    public class HistoryRecord
    {
        public DateTime Timestamp { get; set; }
        public object Value { get; set; }
    }

    public class VariableHistory
    {
        private readonly List<HistoryRecord> _records = new List<HistoryRecord>();
        private const int MAX_RECORDS = 1000; // 可配置

        public void AddRecord(HistoryRecord record)
        {
            _records.Add(record);
            if (_records.Count > MAX_RECORDS)
            {
                _records.RemoveAt(0);
            }
        }

        public List<HistoryRecord> GetRecords(DateTime from, DateTime to)
        {
            return _records.Where(r => r.Timestamp >= from && r.Timestamp <= to).ToList();
        }

        public void Clear()
        {
            _records.Clear();
        }
    }

    #endregion

    #region 公式计算
    
    public class Formula
    {
        public string Expression { get; set; }
        public List<string> DependentVariables { get; set; } = new List<string>();
        
        public double Evaluate(Dictionary<string, double> variables)
        {
            // TODO: 实现公式计算逻辑
            return 0;
        }
    }

    #endregion

    #region 变量管理器

    public class VariableManager
    {
        private readonly Dictionary<string, VariableBase> _variablesByName = new Dictionary<string, VariableBase>();
        private readonly Dictionary<int, VariableBase> _variablesByIndex = new Dictionary<int, VariableBase>();
        private readonly Dictionary<string, List<string>> _groupedVariables = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, string> _aliasMap = new Dictionary<string, string>();
        private readonly Dictionary<string, Formula> _formulas = new Dictionary<string, Formula>();
        
        public event Action<VariableBase> ValueChanged;
        
        // 创建变量
        public VariableBase CreateVariable(string type, string name, int index)
        {
            if (_variablesByName.ContainsKey(name))
                throw new ArgumentException($"Variable {name} already exists");

            VariableBase variable = type switch
            {
                "Numeric" => new NumericVariable(),
                "String" => new StringVariable(),
                "DateTime" => new DateTimeVariable(),
                _ => throw new ArgumentException("Unknown variable type")
            };

            variable.Name = name;
            variable.Index = index;
            
            _variablesByName[name] = variable;
            _variablesByIndex[index] = variable;

            return variable;
        }

        // 添加别名
        public void AddAlias(string variableName, string alias)
        {
            if (!_variablesByName.ContainsKey(variableName))
                throw new KeyNotFoundException($"Variable {variableName} not found");

            if (_aliasMap.ContainsKey(alias))
                throw new ArgumentException($"Alias {alias} already exists");

            _aliasMap[alias] = variableName;
            _variablesByName[variableName].Aliases.Add(alias);
        }

        // 添加到分组
        public void AddToGroup(string groupName, string variableName)
        {
            if (!_variablesByName.ContainsKey(variableName))
                throw new KeyNotFoundException($"Variable {variableName} not found");

            if (!_groupedVariables.ContainsKey(groupName))
                _groupedVariables[groupName] = new List<string>();

            _groupedVariables[groupName].Add(variableName);
            _variablesByName[variableName].Group = groupName;
        }

        // 创建变量快照
        public Dictionary<string, object> CreateSnapshot()
        {
            return _variablesByName.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.GetValue()
            );
        }

        // 从快照恢复
        public void RestoreFromSnapshot(Dictionary<string, object> snapshot)
        {
            foreach (var kvp in snapshot)
            {
                if (_variablesByName.TryGetValue(kvp.Key, out var variable))
                {
                    variable.SetValue(kvp.Value);
                }
            }
        }
    }

    #endregion
}