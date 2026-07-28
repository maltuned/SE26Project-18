using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Models.Requests;

public sealed record CreateTagRequest([Required, StringLength(100)] string Name);
