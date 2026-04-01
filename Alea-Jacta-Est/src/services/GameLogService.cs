using System.Collections.Generic;
using System.Numerics;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Events;

namespace Alea_Jacta_Est.Services;

public class GameLogService
{
    private readonly List<LogEntry> _entries = new();
    private const int MaxEntries = 100;

    public IReadOnlyList<LogEntry> Entries => _entries;

    public record LogEntry(string Message, Vector4 Color);

    public void Add(string message, Vector4 color)
    {
        _entries.Add(new LogEntry(message, color));
        if (_entries.Count > MaxEntries)
            _entries.RemoveAt(0);
    }

    // Event handlers
    public void OnCardPlacedOnBoard(CardPlacedOnBoard e)
    {
        string cardName = e.Card is ValueCard vc ? vc.DisplayName
                        : e.Card is ArcanaCard ac ? ac.ArcanaName
                        : e.Card.TextureRecto.Name;
        Add($"{e.Player.Name} place {cardName} sur le plateau", new Vector4(0.5f, 0.8f, 1f, 1f));
    }

    public void OnCardPlayed(CardPlayed e)
    {
        if (e.Card is ArcanaCard ac)
            Add($"{e.Player.Name} active {ac.ArcanaName}", new Vector4(0.85f, 0.55f, 1f, 1f));
    }

    public void OnTurnValidated(TurnValidated e)
        => Add($"{e.Player.Name} valide son tour", new Vector4(0.7f, 0.7f, 0.7f, 1f));

    public void OnRoundResolved(RoundResolved e)
    {
        foreach (var (player, dmg) in e.Damages)
        {
            if (dmg > 0)
                Add($"{player.Name} recoit {dmg} degats", new Vector4(1f, 0.4f, 0.3f, 1f));
        }
    }

    public void OnPlayerEliminated(PlayerEliminated e)
        => Add($"{e.Player.Name} est elimine!", new Vector4(1f, 0.2f, 0.2f, 1f));

    public void OnEffectApplied(EffectApplied e)
    {
        string cardName = e.Source is ArcanaCard ac ? ac.ArcanaName : e.Source.TextureRecto.Name;
        Add($"Effet: {cardName} active par {e.Owner.Name}", new Vector4(0.85f, 0.55f, 1f, 1f));
    }

    public void OnCardPurchased(CardPurchased e)
    {
        string cardName = e.Card is ValueCard vc ? vc.DisplayName
                        : e.Card is ArcanaCard ac ? ac.ArcanaName
                        : e.Card.TextureRecto.Name;
        Add($"{e.Buyer.Name} achete {cardName} ({e.Price}g)", new Vector4(1f, 0.85f, 0.3f, 1f));
    }
}
