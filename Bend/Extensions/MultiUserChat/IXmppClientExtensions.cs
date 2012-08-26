using System.Runtime.CompilerServices;

namespace Bend.MultiUserChat
{
    public static class IXmppClientExtensions
    {
        private static ConditionalWeakTable<IXmppClient, IClient> extensions = new ConditionalWeakTable<IXmppClient, IClient>();

        public static IClient MultiUserChat(this IXmppClient self)
        {
            return extensions.GetValue(self, CreateMultiUserChatClient);
        }

        private static IClient CreateMultiUserChatClient(IXmppClient xmppClient)
        {
            return new Client(xmppClient);
        }
    }
}
