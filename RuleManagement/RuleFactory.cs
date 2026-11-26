namespace RuleManagement;

using GameManagement;
using PowerManagement;
using ProcessManagement;
using RuleManagement.Dto;
using RuleManagement.Rules;
using SystemManagement;

public class RuleFactory(
    IBatteryMonitor batteryMonitor,
    IProcessMonitor processMonitor,
    IGameMonitor gameMonitor,
    IIdleMonitor idleMonitor,
    IWindowMessageMonitor windowMessageMonitor,
    IPowerManager powerManager,
    ISystemManager systemManager)
{
    private readonly IBatteryMonitor batteryMonitor = batteryMonitor;
    private readonly IProcessMonitor processMonitor = processMonitor;
    private readonly IGameMonitor gameMonitor = gameMonitor;
    private readonly IIdleMonitor idleMonitor = idleMonitor;
    private readonly IWindowMessageMonitor windowMessageMonitor = windowMessageMonitor;
    private readonly IPowerManager powerManager = powerManager;
    private readonly ISystemManager systemManager = systemManager;

    public IRule Create(IRuleDto dto) =>
        dto switch
        {
            ProcessRuleDto processDto => new ProcessRule(processMonitor, processDto),
            GameRuleDto gameRuleDto => new GameRule(gameMonitor, gameRuleDto),
            PowerLineRuleDto powerLineDto => new PowerLineRule(batteryMonitor, powerLineDto),
            IdleRuleDto idleRuleDto => new IdleRule(idleMonitor, powerManager, systemManager, idleRuleDto),
            StartupRuleDto startupRuleDto => new StartupRule(startupRuleDto),
            ShutdownRuleDto shutdownRuleDto => new ShutdownRule(windowMessageMonitor, shutdownRuleDto),
            _ => throw new NotSupportedException($"Unknown rule DTO type: {dto.GetType().Name}")
        };
}
