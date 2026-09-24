using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Integrations.OverlayModules;
using Bloxstrap.Models.APIs.RobloxParty;
using Bloxstrap.Models.APIs.RobloxParty.Events;
using Bloxstrap.Models.Overlay;

namespace Bloxstrap.UI.ViewModels.Overlay.Controls
{
    public class FriendActivityViewModel : NotifyPropertyChangedViewModel
    {
        private readonly RobloxParty? _party;
        private readonly ICollectionView _friendsView;

        private AuthenticatedUser? _current;
        private FriendItem? _selected;

        public ObservableCollection<ChatMessage> MessagesList { get; } = new();

        public ObservableCollection<FriendItem> FriendsList { get; } = new();

        private string _searchText = String.Empty;

        public string SearchTextBoxContent
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;

                _searchText = value;

                OnPropertyChanged(nameof(SearchTextBoxContent));

                _friendsView.Refresh();
            }
        }

        private string _messageText = String.Empty;

        public string MessageTextBoxContent
        {
            get => _messageText;
            set
            {
                if (_messageText == value)
                    return;

                _messageText = value;

                OnPropertyChanged(nameof(MessageTextBoxContent));
            }
        }

        public bool ShowEmptyState => !FriendsList.Any();

        public bool ShowConversationPlaceholder => _selected is null;

        public ICommand SendMessageCommand => new RelayCommand(async () => await SendMessage());

        public FriendActivityViewModel(RobloxParty? party)
        {
            _party = party;

            _friendsView = CollectionViewSource.GetDefaultView(FriendsList);
            _friendsView.Filter = FilterFriends;

            if (_party is not null)
                _party.IncomingMessage += OnIncomingMessage;
        }

        private bool FilterFriends(object item)
        {
            if (item is not FriendItem friend)
                return false;

            return String.IsNullOrEmpty(_searchText)
                || friend.Username.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
        }

        private void OnIncomingMessage(object? sender, MessageEvent message)
        {
            if (_selected is null || _selected.ConversationId != message.ConversationId)
                return;

            _ = App.Current.Dispatcher.InvokeAsync(async () => await RefreshConversation(_selected));
        }

        public async Task LoadConversations()
        {
            const string LOG_IDENT = "FriendActivityViewModel::LoadConversations";

            if (_party is null || !App.Settings.Prop.AllowCookieAccess)
                return;

            try
            {
                if (!App.Cookies.Loaded)
                    await Task.Run(App.Cookies.LoadCookies);

                _current = App.Cookies.CurrentUser;

                if (_current is null)
                    return;

                ConversationsPage? page = await _party.GetConversations();

                if (page is null)
                    return;

                var participants = page.Conversations.SelectMany(x => x.Participants).Distinct().ToList();
                var users = await UserDetails.FetchBatch(participants);

                FriendsList.Clear();

                foreach (Conversation conversation in page.Conversations)
                {
                    long otherUserId = conversation.Participants.FirstOrDefault(x => x != _current.Id);

                    users.TryGetValue(otherUserId, out UserDetails? details);

                    FriendsList.Add(new FriendItem
                    {
                        Username = conversation.Name,
                        StatusText = details?.Data.Name is null ? String.Empty : $"@{details.Data.Name}",
                        ProfileImage = details?.Thumbnail.ImageUrl,
                        ConversationId = conversation.Id,
                        Data = await _party.GetMessages(conversation)
                    });
                }

                OnPropertyChanged(nameof(ShowEmptyState));
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load conversations");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public Task LoadConversationHistory(FriendItem conversation)
        {
            const string LOG_IDENT = "FriendActivityViewModel::LoadConversationHistory";

            _selected = conversation;

            MessagesList.Clear();

            OnPropertyChanged(nameof(ShowConversationPlaceholder));

            if (conversation.Data?.Messages is null)
            {
                App.Logger.WriteLine(LOG_IDENT, $"No history for {conversation.Username}");
                return Task.CompletedTask;
            }

            App.Logger.WriteLine(LOG_IDENT, $"Loading conversation for {conversation.Username}");

            foreach (UserMessage message in conversation.Data.Messages)
                AddMessage(message);

            return Task.CompletedTask;
        }

        private async Task RefreshConversation(FriendItem conversation)
        {
            if (_party is null)
                return;

            conversation.Data = await _party.GetMessages(new Conversation { Id = conversation.ConversationId });

            await LoadConversationHistory(conversation);
        }

        private void AddMessage(UserMessage message)
        {
            if (_selected is null || _current is null)
                return;

            long sender = message.Sender ?? UserMessage.SystemSenderId;

            if (sender == UserMessage.SystemSenderId)
                return;

            if (MessagesList.Any(x => x.MessageId == message.Id))
                return;

            bool isCurrentUser = sender == _current.Id;

            MessagesList.Insert(0, new ChatMessage
            {
                Text = message.Content,
                Sender = isCurrentUser ? "You" : _selected.Username,
                IsCurrentUser = isCurrentUser,
                MessageId = message.Id
            });
        }

        public async Task SendMessage()
        {
            if (_party is null || _selected is null || String.IsNullOrWhiteSpace(_messageText))
                return;

            var pending = new ChatMessage
            {
                Text = _messageText,
                Sender = "You",
                IsCurrentUser = true,
                State = ChatMessageState.Pending
            };

            string outgoing = _messageText;

            MessageTextBoxContent = String.Empty;
            MessagesList.Add(pending);

            try
            {
                await _party.SendMessage(_selected.ConversationId, outgoing);

                pending.State = ChatMessageState.Sent;
            }
            catch (Exception)
            {
                pending.State = ChatMessageState.Failed;
            }
        }
    }
}
