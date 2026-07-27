namespace FaydamPDKS.Core.Enums;

public enum PasswordResetChannel
{
    Email = 1,
    Manager = 2
}

public enum PasswordResetRequestStatus
{
    Pending = 1,
    EmailSent = 2,
    Approved = 3,
    Rejected = 4,
    Completed = 5,
    Expired = 6
}
