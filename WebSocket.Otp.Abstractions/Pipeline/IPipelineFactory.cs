namespace WebSockets.Otp.Abstractions.Pipeline;

public interface IPipelineFactory
{
    ExecutionPipeline CreatePipeline(Type endpoint);
}
