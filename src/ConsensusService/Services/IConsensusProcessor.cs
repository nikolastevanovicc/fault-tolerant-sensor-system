namespace ConsensusService.Services;

public interface IConsensusProcessor
{
    Task ProcessPreviousMinuteAsync(CancellationToken cancellationToken);
}
