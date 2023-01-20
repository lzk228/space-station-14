﻿using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.Botany;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;


namespace Content.Client.Botany.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class PlantExtractorWindow : DefaultWindow
    {
        private readonly IEntityManager _entityManager;
        private readonly IPrototypeManager _prototypeManager;

        private readonly PlantExtractorBoundUserInterface _owner;
        private readonly Dictionary<int, EntityUid> _chamberContentDictionary = new();
        public event Action<BaseButton.ButtonEventArgs, ReagentButton>? OnReagentButtonPressed;
        public PlantExtractorWindow(PlantExtractorBoundUserInterface owner, IEntityManager entityManager, IPrototypeManager prototypeManager)
        {
            _owner = owner;
            _entityManager = entityManager;
            _prototypeManager = prototypeManager;
            RobustXamlLoader.Load(this);

            Tabs.SetTabTitle(0, Loc.GetString("plant-extractor-window-tab-chamber"));
            Tabs.SetTabTitle(1, Loc.GetString("plant-extractor-window-tab-buffer"));

            ExtractButton.OnPressed += _owner.StartExtracting;
            EjectAllButton.OnPressed += _owner.EjectAll;
            ChamberBox.OnItemSelected += OnChamberBoxContentsItemSelected;
            BeakerEjectButton.OnPressed += _owner.EjectBeaker;
            OnReagentButtonPressed += _owner.TransferReagent;
            BufferTransferButton.OnPressed += _owner.SetTransferMode;
            BufferDiscardButton.OnPressed += _owner.SetDiscardMode;
        }

        public void HandleMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case PlantExtractorWorkStartedMessage:
                    ExtractButton.Disabled = true;
                    ExtractButton.Modulate = Color.Green;
                    EjectAllButton.Disabled = true;
                    break;
                case PlantExtractorWorkCompletedMessage:
                    ExtractButton.Disabled = false;
                    ExtractButton.Modulate = Color.White;
                    EjectAllButton.Disabled = false;
                    break;
            }
        }

        public void UpdateState(PlantExtractorBoundUserInterfaceState state)
        {
            BuildChamberUI(state);
            BuildBufferUI(state);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            ExtractButton.OnPressed -= _owner.StartExtracting;
            EjectAllButton.OnPressed -= _owner.EjectAll;
            ChamberBox.OnItemSelected -= OnChamberBoxContentsItemSelected;
            BeakerEjectButton.OnPressed -= _owner.EjectBeaker;
            OnReagentButtonPressed -= _owner.TransferReagent;
            BufferTransferButton.OnPressed -= _owner.SetTransferMode;
            BufferDiscardButton.OnPressed -= _owner.SetDiscardMode;
        }

        private void BuildChamberUI(PlantExtractorBoundUserInterfaceState state)
        {
            ExtractButton.Disabled = !state.CanExtract || state.IsBusy;
            EjectAllButton.Disabled = state.ChamberContent.Length <= 0 || state.IsBusy;

            _chamberContentDictionary.Clear();

            ChamberBox.Clear();
            foreach (var entity in state.ChamberContent)
            {
                if (!_entityManager.EntityExists(entity))
                {
                    continue;
                }

                var texture = _entityManager.GetComponent<SpriteComponent>(entity).Icon?.Default;
                var entityName = _entityManager.GetComponent<MetaDataComponent>(entity).EntityName;

                var solidItem = ChamberBox.AddItem(entityName, texture);
                var solidIndex = ChamberBox.IndexOf(solidItem);
                _chamberContentDictionary.Add(solidIndex, entity);
            }
        }

        private void BuildBufferUI(PlantExtractorBoundUserInterfaceState state)
        {
            BufferTransferButton.Pressed = state.Mode == PlantExtractorMode.Transfer;
            BufferDiscardButton.Pressed = state.Mode == PlantExtractorMode.Discard;

            BuildContainerPanel(state.BeakerContainerInfo);
            BuildBufferPanel(state);
        }

        private void BuildContainerPanel(ContainerInfo? info)
        {
            ContainerInfo.Children.Clear();

            if (info is null)
            {
                ContainerInfo.Children.Add(new Label
                {
                    Text = Loc.GetString("plant-extractor-window-no-container-loaded-text")
                });
                return;
            }

            // Name of the container and its fill status (Ex: 44/100u)
            ContainerInfo.Children.Add(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Children =
                {
                    new Label {Text = $"{info.DisplayName}: "},
                    new Label
                    {
                        Text = $"{info.CurrentVolume}/{info.MaxVolume}",
                    }
                }
            });

            var contents = info.Contents
                .Select(lineItem =>
                {
                    if (!info.HoldsReagents)
                        return (lineItem.Id, lineItem.Id, lineItem.Quantity);

                    // Try to get the prototype for the given reagent. This gives us its name.
                    _prototypeManager.TryIndex(lineItem.Id, out ReagentPrototype? proto);
                    var name = proto?.LocalizedName ?? Loc.GetString("plant-extractor-window-unknown-reagent-text");

                    return (name, lineItem.Id, lineItem.Quantity);

                })
                .OrderBy(r => r.Item1);

            foreach (var (name, id, quantity) in contents)
            {
                var inner = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Children =
                    {
                        new Label { Text = $"{name}: " },
                        new Label
                        {
                            Text = $"{quantity}u"
                        }
                    }
                };

                var cs = inner.Children;

                // Padding
                cs.Add(new Control { HorizontalExpand = true });

                cs.Add(MakeReagentButton("1", PlantExtractorReagentAmount.U1, id, false));
                cs.Add(MakeReagentButton("5", PlantExtractorReagentAmount.U5, id, false));
                cs.Add(MakeReagentButton("10", PlantExtractorReagentAmount.U10, id, false));
                cs.Add(MakeReagentButton("25", PlantExtractorReagentAmount.U25, id, false));
                cs.Add(MakeReagentButton("33", PlantExtractorReagentAmount.U33, id, false));
                cs.Add(MakeReagentButton(
                    Loc.GetString("plant-extractor-window-buffer-all-amount"),
                    PlantExtractorReagentAmount.All, id, false));

                ContainerInfo.Children.Add(inner);
            }
        }

        private void BuildBufferPanel(PlantExtractorBoundUserInterfaceState state)
        {
            BufferInfo.Children.Clear();

            if (!state.BufferReagents.Any())
            {
                BufferInfo.Children.Add(new Label { Text = Loc.GetString("plant-extractor-window-buffer-empty-text") });

                return;
            }

            var bufferHBox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal
            };
            BufferInfo.AddChild(bufferHBox);

            var bufferLabel = new Label { Text = $"{Loc.GetString("plant-extractor-window-buffer-label")} " };
            bufferHBox.AddChild(bufferLabel);
            var bufferVol = new Label
            {
                Text = $"{state.BufferCurrentVolume}u",
            };
            bufferHBox.AddChild(bufferVol);

            foreach (var reagent in state.BufferReagents)
            {
                // Try to get the prototype for the given reagent. This gives us its name.
                _prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto);
                var name = proto?.LocalizedName ?? Loc.GetString("plant-extractor-window-unknown-reagent-text");

                if (proto != null)
                {
                    BufferInfo.Children.Add(new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Label {Text = $"{name}: "},
                            new Label
                            {
                                Text = $"{reagent.Quantity}u",
                            },

                            // Padding
                            new Control {HorizontalExpand = true},

                            MakeReagentButton("1", PlantExtractorReagentAmount.U1, reagent.ReagentId, true),
                            MakeReagentButton("5", PlantExtractorReagentAmount.U5, reagent.ReagentId, true),
                            MakeReagentButton("10", PlantExtractorReagentAmount.U10, reagent.ReagentId, true),
                            MakeReagentButton("25", PlantExtractorReagentAmount.U25, reagent.ReagentId, true),
                            MakeReagentButton("33", PlantExtractorReagentAmount.U33, reagent.ReagentId, true),
                            MakeReagentButton(
                            Loc.GetString("plant-extractor-window-buffer-all-amount"),
                            PlantExtractorReagentAmount.All, reagent.ReagentId, true)
                        }
                    });
                }
            }
        }

        private ReagentButton MakeReagentButton(string text, PlantExtractorReagentAmount amount, string id, bool isBuffer)
        {
            var button = new ReagentButton(text, amount, id, isBuffer);
            button.OnPressed += args => OnReagentButtonPressed?.Invoke(args, button);
            return button;
        }

        private void OnChamberBoxContentsItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            _owner.EjectChamberContent(_chamberContentDictionary[args.ItemIndex]);
        }
    }

    public sealed class ReagentButton : Button
    {
        public PlantExtractorReagentAmount Amount { get; set; }
        public readonly bool IsBuffer;
        public string Id { get; set; }
        public ReagentButton(string text, PlantExtractorReagentAmount amount, string id, bool isBuffer)
        {
            Text = text;
            Amount = amount;
            Id = id;
            IsBuffer = isBuffer;
        }
    }
}
