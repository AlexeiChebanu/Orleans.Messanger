using Orleans.Runtime;
using Orleans.Streams;
using System.IO;

namespace ChatRoom;

public class ChannelGrain : Grain, IChannelGrain
{
    private readonly List<ChatMessage> _messages = new(100);
    private readonly List<string> _onlineMembers = new(10);

    private IAsyncStream<ChatMessage> _stream = null!;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var streamProvider = this.GetStreamProvider("chat");

        var streamId = StreamId.Create(
            "ChatRoom", this.GetPrimaryKeyString());

        _stream = streamProvider.GetStream<ChatMessage>(
            streamId);

        return base.OnActivateAsync(cancellationToken);
    }

    public async Task<StreamId> Join(string nickname)
    {
        _onlineMembers.Add(nickname);

        await _stream.OnNextAsync(
            new ChatMessage(
                "System",
                $"{nickname} joins the chat '{this.GetPrimaryKeyString()}' ..."));

        return _stream.StreamId;
    }

    public async Task<StreamId> Leave(string nickname)
    {
        _onlineMembers.Remove(nickname);

        await _stream.OnNextAsync(
            new ChatMessage(
                "System",
                $"{nickname} leaves the chat..."));

        return _stream.StreamId;
    }

    public async Task<bool> Message(ChatMessage msg)
    {
        _messages.Add(msg);

        await _stream.OnNextAsync(msg);

        return true;
    }

    public Task<string[]> GetMembers() => Task.FromResult(_onlineMembers.ToArray());

    public Task<ChatMessage[]> ReadHistory(int numberOfMessages)
    {
        var response = _messages
            .OrderByDescending(x => x.Created)
            .Take(numberOfMessages)
            .OrderBy(x => x.Created)
            .ToArray();

        return Task.FromResult(response);
    }
}
