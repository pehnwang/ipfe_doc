# impl

明白了，从这两个文件中我看到了一些重要信息：

1. 变量存储系统 (FrmValue.cs):
   - 使用Dictionary<string, DicValue>存储变量
   - 支持多种数据类型(double, string, bool)
   - 带有索引和名称两种访问方式
   - 目前与UI强耦合

2. 通信系统 (FrmComunication.cs):
   - 支持TCP客户端/服务器
   - 支持串口通信
   - 提供异步通信机制
   - 同样与UI强耦合

让我们先实现流程引擎的核心部分，将这些功能解耦。我会创建一个基础的变量存储实现和设备通信适配器。

现在我们有了基础的实现，接下来需要开发流程引擎的核心实现。在此之前，我需要确认几点：

1. 关于变量存储：
   - 是否需要保持与现有FrmValue完全兼容？
   - 是否需要支持变量的持久化存储？

2. 关于设备通信：
   - TCP和串口通信是否还有其他特殊需求？
   - 是否需要支持其他类型的通信协议？

3. 关于DataRun类：
   - 现有的ProcHandle类是否需要保留其所有功能？
   - 对于异步操作的处理方式是否需要改变？

``` csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IPFE.Core.Implementations
{
    /// <summary>
    /// 变量存储实现
    /// </summary>
    public class VariableStorage : IVariableStorage
    {
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>();
        private readonly object _lock = new object();

        public T GetValue<T>(string name)
        {
            lock (_lock)
            {
                if (_variables.TryGetValue(name, out object value))
                {
                    try 
                    {
                        if(value is T typedValue)
                        {
                            return typedValue;
                        }
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch 
                    {
                        throw new InvalidCastException($"Cannot convert variable '{name}' to type {typeof(T)}");
                    }
                }
                throw new KeyNotFoundException($"Variable '{name}' not found");
            }
        }

        public void SetValue<T>(string name, T value)
        {
            lock (_lock)
            {
                _variables[name] = value;
            }
        }

        public bool ContainsVariable(string name)
        {
            lock (_lock)
            {
                return _variables.ContainsKey(name);
            }
        }

        public void RemoveVariable(string name)
        {
            lock (_lock)
            {
                _variables.Remove(name);
            }
        }
    }

    /// <summary>
    /// 设备通信适配器基类
    /// </summary>
    public abstract class DeviceCommunicationBase : IDeviceCommunication
    {
        protected readonly Dictionary<string, object> _devices = new Dictionary<string, object>();
        protected readonly object _lock = new object();

        public abstract bool Connect(string deviceId);
        public abstract void Disconnect(string deviceId);
        public abstract bool IsConnected(string deviceId);
        public abstract Task<bool> SendCommand(string deviceId, string command, object parameters);
        public abstract Task<T> ReadValue<T>(string deviceId, string address);
        public abstract Task<bool> WriteValue<T>(string deviceId, string address, T value);

        protected bool DeviceExists(string deviceId)
        {
            lock (_lock)
            {
                return _devices.ContainsKey(deviceId);
            }
        }

        protected void RegisterDevice(string deviceId, object device)
        {
            lock (_lock)
            {
                _devices[deviceId] = device;
            }
        }

        protected void UnregisterDevice(string deviceId)
        {
            lock (_lock)
            {
                _devices.Remove(deviceId);
            }
        }
    }

    /// <summary>
    /// TCP通信适配器
    /// </summary>
    public class TcpCommunication : DeviceCommunicationBase
    {
        public override bool Connect(string deviceId)
        {
            // 实现TCP连接逻辑
            return true;
        }

        public override void Disconnect(string deviceId)
        {
            // 实现TCP断开逻辑
        }

        public override bool IsConnected(string deviceId)
        {
            // 实现TCP连接状态检查
            return true;
        }

        public override Task<bool> SendCommand(string deviceId, string command, object parameters)
        {
            // 实现TCP发送命令
            return Task.FromResult(true);
        }

        public override Task<T> ReadValue<T>(string deviceId, string address)
        {
            // 实现TCP读取值
            return Task.FromResult(default(T));
        }

        public override Task<bool> WriteValue<T>(string deviceId, string address, T value)
        {
            // 实现TCP写入值
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// 串口通信适配器
    /// </summary>
    public class SerialPortCommunication : DeviceCommunicationBase
    {
        public override bool Connect(string deviceId)
        {
            // 实现串口连接逻辑
            return true;
        }

        public override void Disconnect(string deviceId)
        {
            // 实现串口断开逻辑
        }

        public override bool IsConnected(string deviceId)
        {
            // 实现串口连接状态检查
            return true;
        }

        public override Task<bool> SendCommand(string deviceId, string command, object parameters)
        {
            // 实现串口发送命令
            return Task.FromResult(true);
        }

        public override Task<T> ReadValue<T>(string deviceId, string address)
        {
            // 实现串口读取值
            return Task.FromResult(default(T));
        }

        public override Task<bool> WriteValue<T>(string deviceId, string address, T value)
        {
            // 实现串口写入值
            return Task.FromResult(true);
        }
    }
}

```
