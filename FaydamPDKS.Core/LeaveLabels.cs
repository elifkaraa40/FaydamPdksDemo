using FaydamPDKS.Core.Enums;

namespace FaydamPDKS.Core;

public static class LeaveLabels
{
    public static string Type(LeaveType value) => value switch
    {
        LeaveType.Annual => "Yıllık izin", LeaveType.Sick => "Hastalık izni",
        LeaveType.Excuse => "Mazeret izni", LeaveType.Unpaid => "Ücretsiz izin", _ => value.ToString()
    };
    public static string Status(LeaveRequestStatus value) => value switch
    {
        LeaveRequestStatus.Pending => "Bekliyor", LeaveRequestStatus.Approved => "Onaylandı",
        LeaveRequestStatus.Rejected => "Reddedildi", LeaveRequestStatus.Cancelled => "İptal edildi", _ => value.ToString()
    };
    public static string Portion(LeaveDayPortion value) => value switch
    {
        LeaveDayPortion.FullDay => "Tam gün", LeaveDayPortion.FirstHalf => "İlk yarım gün",
        LeaveDayPortion.SecondHalf => "İkinci yarım gün", _ => value.ToString()
    };
}
