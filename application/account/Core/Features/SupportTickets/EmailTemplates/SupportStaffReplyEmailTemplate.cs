using SharedKernel.Emails;

namespace Account.Features.SupportTickets.EmailTemplates;

public sealed record SupportStaffReplyEmailTemplate(string Locale, SupportStaffReplyEmailModel Data)
    : EmailTemplateBase("SupportStaffReply", Locale, Data);

public sealed record SupportStaffReplyEmailModel(string StaffName, string ShortDisplayId, string Subject, string Body, string TicketUrl);
