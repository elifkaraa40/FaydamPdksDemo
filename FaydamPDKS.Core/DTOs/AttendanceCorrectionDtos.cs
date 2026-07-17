using FaydamPDKS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace FaydamPDKS.Core.DTOs;

public sealed class CreateAttendanceCorrectionDto
{
    public AttendanceCorrectionType CorrectionType { get; set; } = AttendanceCorrectionType.TimeCorrection;
    [Required] public DateOnly WorkDate { get; set; }
    [Required] public TimeOnly RequestedEntry { get; set; }
    [Required] public TimeOnly RequestedExit { get; set; }
    [Required, StringLength(500, MinimumLength = 10)] public string Reason { get; set; } = string.Empty;
    [StringLength(150)] public string? ProjectName { get; set; }
    [StringLength(150)] public string? CustomerName { get; set; }
    [StringLength(300)] public string? FieldAddress { get; set; }
}

public sealed record AttendanceCorrectionDto(Guid Id, DateOnly WorkDate, TimeOnly RequestedEntry, TimeOnly RequestedExit, string Reason, AttendanceCorrectionStatus Status, DateTimeOffset CreatedAt, string? ReviewNote, AttendanceCorrectionType CorrectionType = AttendanceCorrectionType.TimeCorrection, string? ProjectName = null, string? CustomerName = null, string? FieldAddress = null);
public sealed record AttendanceCorrectionReviewDto(Guid Id, Guid UserId, string EmployeeName, string EmployeeNumber, DateOnly WorkDate, TimeOnly RequestedEntry, TimeOnly RequestedExit, string Reason, AttendanceCorrectionStatus Status, DateTimeOffset CreatedAt, string? ReviewNote, AttendanceCorrectionType CorrectionType = AttendanceCorrectionType.TimeCorrection, string? ProjectName = null, string? CustomerName = null, string? FieldAddress = null);
public sealed record ReviewAttendanceCorrectionDto(bool Approve, [StringLength(500)] string? Note);
