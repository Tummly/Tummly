using TummlyBackend.Services;

namespace TummlyBackend.Interfaces
{
    public interface IActivationGate
    {
        ActivationDecision Decide(
            ActivationSubject subject,
            ActivationIntent intent
        );
    }
}
