using System.Numerics;
using ImGuiNET;
using Alea_Jacta_Est.Commands;
using Alea_Jacta_Est.Effects;
using Alea_Jacta_Est.Entities;
using Alea_Jacta_Est.Main;

namespace Alea_Jacta_Est.Services;

public class TargetSelectionWindowService
{
    private readonly CommandQueue _commands;
    private readonly EffectManager _effectManager;
    private int _selectedTargetIndex = -1;

    public TargetSelectionWindowService(CommandQueue commands, EffectManager effectManager)
    {
        _commands = commands;
        _effectManager = effectManager;
    }

    public void Render(GameState state)
    {
        if (state.PendingActivation == null) return;

        var pending = state.PendingActivation;

        ImGui.SetNextWindowSize(new Vector2(320, 220), ImGuiCond.Always);
        ImGui.SetNextWindowPos(new Vector2(400, 300), ImGuiCond.Always);
        ImGui.OpenPopup("Sélectionner une cible");

        if (!ImGui.BeginPopupModal("Sélectionner une cible", ref _modalOpen,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
            return;

        ImGui.TextUnformatted($"Activer : {pending.Card.ArcanaName} ({(pending.Card.IsUpright ? "Endroit" : "Envers")})");
        ImGui.Separator();
        ImGui.TextUnformatted("Sélectionnez un adversaire :");
        ImGui.Spacing();

        var opponents = state.Players.FindAll(p => p != pending.Activator);
        for (int i = 0; i < opponents.Count; i++)
        {
            var opponent = opponents[i];
            bool selected = _selectedTargetIndex == i;
            if (ImGui.Selectable($"{opponent.Name}  ({opponent.Health} PV)##{i}", selected))
                _selectedTargetIndex = i;
        }

        ImGui.Spacing();
        ImGui.Separator();

        bool hasTarget = _selectedTargetIndex >= 0 && _selectedTargetIndex < opponents.Count;
        if (!hasTarget) ImGui.BeginDisabled();
        if (ImGui.Button("Confirmer") && hasTarget)
        {
            _commands.Enqueue(new ActivateArcanaWithTargetCommand(
                pending.Activator,
                pending.Card,
                opponents[_selectedTargetIndex],
                _effectManager));
            _selectedTargetIndex = -1;
            ImGui.CloseCurrentPopup();
        }
        if (!hasTarget) ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Annuler"))
        {
            state.PendingActivation = null;
            _selectedTargetIndex = -1;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    // Dummy ref required by BeginPopupModal signature
    private bool _modalOpen = true;
}
