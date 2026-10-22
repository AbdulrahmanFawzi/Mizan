using Mizan.Domain.Common;

namespace Mizan.Domain.Insights;

public abstract record Insight(Guid CategoryId);

// "You spent 27% more than your Food budget" (BR-N1). PercentOver is null when the limit is zero.
public sealed record OverBudgetInsight(Guid CategoryId, Money OverBy, decimal? PercentOver) : Insight(CategoryId);

// "At your current pace you will exceed your Fuel budget" (BR-N4).
public sealed record ProjectedOverBudgetInsight(Guid CategoryId, Money Projected, Money Limit) : Insight(CategoryId);
