namespace Vellum.Validator.Models;

public record LayerMetrics(
    string Layer,
    int AfferentCoupling,
    int EfferentCoupling,
    double Instability,
    double Abstraction,
    double Distance
);
