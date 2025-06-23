namespace CRM_Api.Models
{
    public class Chat
    {
        public class ChatMessageDto
        {
            public string SenderUsername { get; set; }
            public string ReceiverUsername { get; set; }
            public string MessageText { get; set; }
        }
        public class UserDto
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
        }

    }
}
