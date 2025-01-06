# ipfe core interface

> 这是流程引擎的核心接口设计，主要包含以下几个部分：

1. 核心概念定义：
   - FlowStatus：流程状态枚举
   - IFlowContext：流程执行上下文
   - IFlowEngine：流程引擎接口
   - IInstruction：指令接口
   - IInstructionHandler：指令处理器接口

2. 支持接口：
   - IVariableStorage：变量存储接口
   - IDeviceCommunication：设备通信接口

接下来，我们需要实现流程引擎的具体类。在开始之前，您对这些接口设计有什么想法或建议吗？另外，为了更好地实现流程引擎，我是否需要了解更多关于：

1. 现有的变量存储机制
2. 设备通信的具体实现方式
3. 其他相关的依赖类

``` csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IPFE.Core
{
    /// <summary>
    /// 流程状态枚举
    /// </summary>
    public enum FlowStatus
    {
        Ready,      // 就绪
        Running,    // 运行中
        Paused,     // 暂停
        Stopped,    // 停止
        Error       // 错误
    }

    /// <summary>
    /// 流程执行上下文接口
    /// </summary>
    public interface IFlowContext
    {
        /// <summary>
        /// 流程ID
        /// </summary>
        string FlowId { get; }
        
        /// <summary>
        /// 当前状态
        /// </summary>
        FlowStatus Status { get; }
        
        /// <summary>
        /// 变量存储
        /// </summary>
        IVariableStorage Variables { get; }
        
        /// <summary>
        /// 设备通信接口
        /// </summary>
        IDeviceCommunication DeviceCommunication { get; }
    }

    /// <summary>
    /// 流程引擎接口
    /// </summary>
    public interface IFlowEngine
    {
        /// <summary>
        /// 启动流程
        /// </summary>
        Task<bool> StartFlow(string flowId);
        
        /// <summary>
        /// 暂停流程
        /// </summary>
        Task<bool> PauseFlow(string flowId);
        
        /// <summary>
        /// 恢复流程
        /// </summary>
        Task<bool> ResumeFlow(string flowId);
        
        /// <summary>
        /// 停止流程
        /// </summary>
        Task<bool> StopFlow(string flowId);
        
        /// <summary>
        /// 获取流程状态
        /// </summary>
        FlowStatus GetFlowStatus(string flowId);
        
        /// <summary>
        /// 注册指令处理器
        /// </summary>
        void RegisterInstructionHandler(string instructionType, IInstructionHandler handler);
    }

    /// <summary>
    /// 指令处理器接口
    /// </summary>
    public interface IInstructionHandler
    {
        /// <summary>
        /// 执行指令
        /// </summary>
        Task<bool> Execute(IInstruction instruction, IFlowContext context);
        
        /// <summary>
        /// 验证指令
        /// </summary>
        bool Validate(IInstruction instruction);
    }

    /// <summary>
    /// 指令接口
    /// </summary>
    public interface IInstruction
    {
        /// <summary>
        /// 指令ID
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// 指令类型
        /// </summary>
        string Type { get; }
        
        /// <summary>
        /// 指令参数
        /// </summary>
        IDictionary<string, object> Parameters { get; }
        
        /// <summary>
        /// 是否启用
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// 是否是断点
        /// </summary>
        bool IsBreakpoint { get; set; }
    }

    /// <summary>
    /// 变量存储接口
    /// </summary>
    public interface IVariableStorage
    {
        T GetValue<T>(string name);
        void SetValue<T>(string name, T value);
        bool ContainsVariable(string name);
        void RemoveVariable(string name);
    }

    /// <summary>
    /// 设备通信接口
    /// </summary>
    public interface IDeviceCommunication
    {
        bool Connect(string deviceId);
        void Disconnect(string deviceId);
        bool IsConnected(string deviceId);
        Task<bool> SendCommand(string deviceId, string command, object parameters);
        Task<T> ReadValue<T>(string deviceId, string address);
        Task<bool> WriteValue<T>(string deviceId, string address, T value);
    }

    /// <summary>
    /// 流程执行异常
    /// </summary>
    public class FlowExecutionException : Exception
    {
        public string FlowId { get; }
        public string InstructionId { get; }

        public FlowExecutionException(string message, string flowId, string instructionId, Exception innerException = null)
            : base(message, innerException)
        {
            FlowId = flowId;
            InstructionId = instructionId;
        }
    }
}

```
