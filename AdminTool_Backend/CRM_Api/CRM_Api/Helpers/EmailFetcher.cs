using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using Microsoft.Data.SqlClient;

public class EmailFetcher
{
    private readonly string _imapServer;
    private readonly int _imapPort;
    private readonly string _username;
    private readonly string _password;
    private readonly string _connectionString;

    public EmailFetcher(string imapServer, int imapPort, string username, string password, string connectionString)
    {
        _imapServer = imapServer;
        _imapPort = imapPort;
        _username = username;
        _password = password;
        _connectionString = connectionString;
    }

    public void FetchAndStoreEmails()
    {
        using var client = new ImapClient();
        client.Connect(_imapServer, _imapPort, true);
        client.Authenticate(_username, _password);

        client.Inbox.Open(MailKit.FolderAccess.ReadOnly);

        var uids = client.Inbox.Search(SearchQuery.All);
        foreach (var uid in uids)
        {
            var message = client.Inbox.GetMessage(uid);

            // Check if message already exists in DB
            if (IsMessageExists(message.MessageId))
                continue;

            SaveEmailToDatabase(message);
        }

        client.Disconnect(true);
    }

    private bool IsMessageExists(string messageId)
    {
        if (string.IsNullOrEmpty(messageId)) return false;

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM InboxEmails WHERE MessageId = @MessageId", conn);
        cmd.Parameters.AddWithValue("@MessageId", messageId);

        conn.Open();
        int count = (int)cmd.ExecuteScalar();
        return count > 0;
    }

    private void SaveEmailToDatabase(MimeMessage message)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        var attachmentNames = message.Attachments
            .Select(att => att.ContentDisposition?.FileName ?? att.ContentType?.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();

        var joinedNames = string.Join(",", attachmentNames);

        var cmd = new SqlCommand(@"
            INSERT INTO InboxEmails (ReceivedTime, FromEmail, ToEmail, Subject, Body, AttachmentNames, MessageId)
            VALUES (@ReceivedTime, @FromEmail, @ToEmail, @Subject, @Body, @AttachmentNames, @MessageId)", conn);

        cmd.Parameters.AddWithValue("@ReceivedTime", message.Date.DateTime);
        cmd.Parameters.AddWithValue("@FromEmail", message.From.ToString());
        cmd.Parameters.AddWithValue("@ToEmail", message.To.ToString());
        cmd.Parameters.AddWithValue("@Subject", message.Subject ?? string.Empty);
        cmd.Parameters.AddWithValue("@Body", message.TextBody ?? message.HtmlBody ?? string.Empty);
        cmd.Parameters.AddWithValue("@AttachmentNames", joinedNames);
        cmd.Parameters.AddWithValue("@MessageId", message.MessageId ?? Guid.NewGuid().ToString());

        cmd.ExecuteNonQuery();
    }
}
