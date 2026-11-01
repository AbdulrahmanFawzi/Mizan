namespace Mizan.Domain.Banking;

// BR-C3. Only Active connections are synced.
public enum ConnectionStatus
{
    Active,
    Expired,
    Revoked,
}
