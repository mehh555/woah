using System;

namespace Woah.Api.Contracts.Sessions;

public class SubmitAnswerRequest
{
    public Guid PlayerId { get; set; }
    public string Answer { get; set; } = default!;
}