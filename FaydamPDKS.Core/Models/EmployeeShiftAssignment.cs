using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FaydamPDKS.Core.Models;

[Table("employee_shift_assignments")]
public sealed class EmployeeShiftAssignment
{
    [Key, Column("id")]
    public Guid Id { get; set; }

    [Column("employee_id")]
    public Guid EmployeeId { get; set; }
    public User? Employee { get; set; }

    [Column("shift_id")]
    public Guid ShiftId { get; set; }
    public Shift? Shift { get; set; }

    [Column("valid_from")]
    public DateOnly ValidFrom { get; set; }

    [Column("valid_to")]
    public DateOnly? ValidTo { get; set; }
}
