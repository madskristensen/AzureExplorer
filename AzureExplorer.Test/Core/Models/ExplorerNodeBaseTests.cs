using AzureExplorer.Core.Models;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AzureExplorer.Test.Core.Models
{
    [TestClass]
    public sealed class ExplorerNodeBaseTests
    {
        [TestMethod]
        public void Label_Get_ReturnsCorrectValue()
        {
            // Arrange
            var node = new TestExplorerNode("Initial Label");

            // Act
            var label = node.Label;

            // Assert
            Assert.AreEqual("Initial Label", label);
        }

        [TestMethod]
        public void Label_Set_UpdatesValue()
        {
            // Arrange
            var node = new TestExplorerNode("Initial Label")
            {
                // Act
                Label = "New Label"
            };

            // Assert
            Assert.AreEqual("New Label", node.Label);
        }

        [TestMethod]
        public void Label_Set_RaisesPropertyChangedEvent()
        {
            // Arrange
            var node = new TestExplorerNode("Initial Label");
            var eventRaised = false;
            string? propertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                propertyName = args.PropertyName;
            };

            // Act
            node.Label = "New Label";

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("Label", propertyName);
        }

        [TestMethod]
        public void Label_SetSameValue_DoesNotRaisePropertyChangedEvent()
        {
            // Arrange
            var node = new TestExplorerNode("Initial Label");
            var eventRaised = false;
            node.PropertyChanged += (sender, args) => eventRaised = true;

            // Act
            node.Label = "Initial Label";

            // Assert
            Assert.IsFalse(eventRaised);
        }

        [TestMethod]
        public void Description_Get_ReturnsCorrectValue()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                Description = "Test Description"
            };

            // Act
            var description = node.Description;

            // Assert
            Assert.AreEqual("Test Description", description);
        }

        [TestMethod]
        public void Description_Set_UpdatesValue()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                // Act
                Description = "New Description"
            };

            // Assert
            Assert.AreEqual("New Description", node.Description);
        }

        [TestMethod]
        public void Description_Set_RaisesPropertyChangedEvent()
        {
            // Arrange
            var node = new TestExplorerNode("Label");
            var eventRaised = false;
            string? propertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                propertyName = args.PropertyName;
            };

            // Act
            node.Description = "Description";

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("Description", propertyName);
        }

        [TestMethod]
        public void Description_SetSameValue_DoesNotRaisePropertyChangedEvent()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                Description = "Same Description"
            };
            var eventRaised = false;
            node.PropertyChanged += (sender, args) => eventRaised = true;

            // Act
            node.Description = "Same Description";

            // Assert
            Assert.IsFalse(eventRaised);
        }

        [TestMethod]
        public void IsExpanded_Get_ReturnsCorrectValue()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                IsExpanded = true
            };

            // Act
            var isExpanded = node.IsExpanded;

            // Assert
            Assert.IsTrue(isExpanded);
        }

        [TestMethod]
        public void IsExpanded_Set_UpdatesValue()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                // Act
                IsExpanded = true
            };

            // Assert
            Assert.IsTrue(node.IsExpanded);
        }

        [TestMethod]
        public void IsExpanded_Set_RaisesPropertyChangedEvent()
        {
            // Arrange
            var node = new TestExplorerNode("Label");
            var eventRaised = false;
            string? propertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                propertyName = args.PropertyName;
            };

            // Act
            node.IsExpanded = true;

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("IsExpanded", propertyName);
        }

        [TestMethod]
        public void IsExpanded_SetSameValue_DoesNotRaisePropertyChangedEvent()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                IsExpanded = true
            };
            var eventRaised = false;
            node.PropertyChanged += (sender, args) => eventRaised = true;

            // Act
            node.IsExpanded = true;

            // Assert
            Assert.IsFalse(eventRaised);
        }

        [TestMethod]
        public void IsLoading_Get_ReturnsCorrectValue()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                IsLoading = true
            };

            // Act
            var isLoading = node.IsLoading;

            // Assert
            Assert.IsTrue(isLoading);
        }

        [TestMethod]
        public void IsLoading_Set_UpdatesValue()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                // Act
                IsLoading = true
            };

            // Assert
            Assert.IsTrue(node.IsLoading);
        }

        [TestMethod]
        public void IsLoading_Set_RaisesPropertyChangedEvent()
        {
            // Arrange
            var node = new TestExplorerNode("Label");
            var eventRaised = false;
            string? propertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                propertyName = args.PropertyName;
            };

            // Act
            node.IsLoading = true;

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("IsLoading", propertyName);
        }

        [TestMethod]
        public void IsLoading_SetSameValue_DoesNotRaisePropertyChangedEvent()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                IsLoading = true
            };
            var eventRaised = false;
            node.PropertyChanged += (sender, args) => eventRaised = true;

            // Act
            node.IsLoading = true;

            // Assert
            Assert.IsFalse(eventRaised);
        }

        [TestMethod]
        public void IsLoaded_Get_ReturnsCorrectValue()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                IsLoaded = true
            };

            // Act
            var isLoaded = node.IsLoaded;

            // Assert
            Assert.IsTrue(isLoaded);
        }

        [TestMethod]
        public void IsLoaded_Set_UpdatesValue()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                // Act
                IsLoaded = true
            };

            // Assert
            Assert.IsTrue(node.IsLoaded);
        }

        [TestMethod]
        public void IsLoaded_Set_RaisesPropertyChangedEvent()
        {
            // Arrange
            var node = new TestExplorerNode("Label");
            var eventRaised = false;
            string? propertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                propertyName = args.PropertyName;
            };

            // Act
            node.IsLoaded = true;

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("IsLoaded", propertyName);
        }

        [TestMethod]
        public void IsLoaded_SetSameValue_DoesNotRaisePropertyChangedEvent()
        {
            // Arrange
            var node = new TestExplorerNode("Label")
            {
                IsLoaded = true
            };
            var eventRaised = false;
            node.PropertyChanged += (sender, args) => eventRaised = true;

            // Act
            node.IsLoaded = true;

            // Assert
            Assert.IsFalse(eventRaised);
        }

        [TestMethod]
        public async Task RefreshAsync_ClearsState_AndCallsLoadChildrenAsync()
        {
            // Arrange
            var node = new TestExplorerNodeWithLoadTracking("Label")
            {
                IsLoaded = true,
                IsLoading = true,
                Description = "Test Description"
            };
            node.Children.Add(new TestExplorerNode("Child1"));
            node.Children.Add(new TestExplorerNode("Child2"));

            // Act
            await node.RefreshAsync(TestContext.CancellationToken);

            // Assert
            Assert.IsFalse(node.IsLoaded);
            Assert.IsFalse(node.IsLoading);
            Assert.IsNull(node.Description);
            Assert.IsEmpty(node.Children);
            Assert.AreEqual(1, node.LoadChildrenCallCount);
        }

        [TestMethod]
        public async Task RefreshAsync_WithCancellationToken_PassesToLoadChildrenAsync()
        {
            // Arrange
            var node = new TestExplorerNodeWithLoadTracking("Label");
            var cancellationToken = new CancellationToken(true);

            // Act
            await node.RefreshAsync(cancellationToken);

            // Assert
            Assert.IsTrue(node.LastCancellationToken.IsCancellationRequested);
        }

        [TestMethod]
        public void BeginLoading_WhenNotLoadedOrLoading_SetsLoadingState()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");

            // Act
            var result = node.BeginLoadingPublic();

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(node.IsLoading);
            Assert.AreEqual("loading...", node.Description);
        }

        [TestMethod]
        public void BeginLoading_WhenAlreadyLoaded_ReturnsFalse()
        {
            // Arrange
            var node = new TestableExplorerNode("Label")
            {
                IsLoaded = true
            };

            // Act
            var result = node.BeginLoadingPublic();

            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(node.IsLoading);
            Assert.IsNull(node.Description);
        }

        [TestMethod]
        public void BeginLoading_WhenAlreadyLoading_ReturnsFalse()
        {
            // Arrange
            var node = new TestableExplorerNode("Label")
            {
                IsLoading = true
            };

            // Act
            var result = node.BeginLoadingPublic();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void BeginLoading_RemovesLoadingNodes_KeepsOtherChildren()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            node.Children.Add(new TestExplorerNode("Child1"));
            node.Children.Add(new LoadingNode());
            node.Children.Add(new TestExplorerNode("Child2"));
            node.Children.Add(new LoadingNode());

            // Act
            var result = node.BeginLoadingPublic();

            // Assert
            Assert.IsTrue(result);
            Assert.HasCount(2, node.Children);
            Assert.AreEqual("Child1", node.Children[0].Label);
            Assert.AreEqual("Child2", node.Children[1].Label);
        }

        [TestMethod]
        public void BeginLoading_WithOnlyLoadingNodes_RemovesAll()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            node.Children.Add(new LoadingNode());
            node.Children.Add(new LoadingNode());
            node.Children.Add(new LoadingNode());

            // Act
            var result = node.BeginLoadingPublic();

            // Assert
            Assert.IsTrue(result);
            Assert.IsEmpty(node.Children);
        }

        [TestMethod]
        public void BeginLoading_WithNoChildren_Succeeds()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");

            // Act
            var result = node.BeginLoadingPublic();

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(node.IsLoading);
            Assert.AreEqual("loading...", node.Description);
        }

        [TestMethod]
        public void EndLoading_SetsLoadedStateAndClearsDescription()
        {
            // Arrange
            var node = new TestableExplorerNode("Label")
            {
                IsLoading = true,
                Description = "loading..."
            };

            // Act
            node.EndLoadingPublic();

            // Assert
            Assert.IsTrue(node.IsLoaded);
            Assert.IsFalse(node.IsLoading);
            Assert.IsNull(node.Description);
        }

        [TestMethod]
        public void EndLoading_WhenNotLoading_StillSetsLoadedState()
        {
            // Arrange
            var node = new TestableExplorerNode("Label")
            {
                Description = "Some description"
            };

            // Act
            node.EndLoadingPublic();

            // Assert
            Assert.IsTrue(node.IsLoaded);
            Assert.IsFalse(node.IsLoading);
            Assert.IsNull(node.Description);
        }

        [TestMethod]
        public void AddChild_SetsParentAndAddsToChildren()
        {
            // Arrange
            var parent = new TestableExplorerNode("Parent");
            var child = new TestExplorerNode("Child");

            // Act
            parent.AddChildPublic(child);

            // Assert
            Assert.AreEqual(parent, child.Parent);
            Assert.HasCount(1, parent.Children);
            Assert.AreEqual(child, parent.Children[0]);
        }

        [TestMethod]
        public void AddChild_WithMultipleChildren_MaintainsOrder()
        {
            // Arrange
            var parent = new TestableExplorerNode("Parent");
            var child1 = new TestExplorerNode("Child1");
            var child2 = new TestExplorerNode("Child2");
            var child3 = new TestExplorerNode("Child3");

            // Act
            parent.AddChildPublic(child1);
            parent.AddChildPublic(child2);
            parent.AddChildPublic(child3);

            // Assert
            Assert.HasCount(3, parent.Children);
            Assert.AreEqual("Child1", parent.Children[0].Label);
            Assert.AreEqual("Child2", parent.Children[1].Label);
            Assert.AreEqual("Child3", parent.Children[2].Label);
            Assert.AreEqual(parent, child1.Parent);
            Assert.AreEqual(parent, child2.Parent);
            Assert.AreEqual(parent, child3.Parent);
        }

        [TestMethod]
        public void SetProperty_WithDifferentValue_UpdatesFieldAndRaisesEvent()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            var field = 10;
            var eventRaised = false;
            string? raisedPropertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                raisedPropertyName = args.PropertyName;
            };

            // Act
            var result = node.SetPropertyPublic(ref field, 20, "TestProperty");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(20, field);
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("TestProperty", raisedPropertyName);
        }

        [TestMethod]
        public void SetProperty_WithSameValue_DoesNotUpdateFieldOrRaiseEvent()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            var field = 10;
            var eventRaised = false;
            node.PropertyChanged += (sender, args) => eventRaised = true;

            // Act
            var result = node.SetPropertyPublic(ref field, 10, "TestProperty");

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(10, field);
            Assert.IsFalse(eventRaised);
        }

        [TestMethod]
        public void SetProperty_WithNullToNull_DoesNotRaiseEvent()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            string? field = null;
            var eventRaised = false;
            node.PropertyChanged += (sender, args) => eventRaised = true;

            // Act
            var result = node.SetPropertyPublic(ref field, null!, "TestProperty");

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(field);
            Assert.IsFalse(eventRaised);
        }

        [TestMethod]
        public void SetProperty_WithNullToValue_UpdatesFieldAndRaisesEvent()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            string? field = null;
            var eventRaised = false;
            string? raisedPropertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                raisedPropertyName = args.PropertyName;
            };

            // Act
            var result = node.SetPropertyPublic(ref field, "NewValue", "TestProperty");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("NewValue", field);
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("TestProperty", raisedPropertyName);
        }

        [TestMethod]
        public void SetProperty_WithValueToNull_UpdatesFieldAndRaisesEvent()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            var field = "OldValue";
            var eventRaised = false;
            string? raisedPropertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                raisedPropertyName = args.PropertyName;
            };

            // Act
            var result = node.SetPropertyPublic(ref field, null!, "TestProperty");

            // Assert
            Assert.IsTrue(result);
            Assert.IsNull(field);
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("TestProperty", raisedPropertyName);
        }

        [TestMethod]
        public void SetProperty_WithNoEventHandler_UpdatesFieldWithoutException()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            var field = 10;

            // Act
            var result = node.SetPropertyPublic(ref field, 20, "TestProperty");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(20, field);
        }

        [TestMethod]
        public void SetProperty_WithEmptyPropertyName_RaisesEventWithEmptyName()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            var field = 10;
            var eventRaised = false;
            string? raisedPropertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                raisedPropertyName = args.PropertyName;
            };

            // Act
            var result = node.SetPropertyPublic(ref field, 20, string.Empty);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(20, field);
            Assert.IsTrue(eventRaised);
            Assert.AreEqual(string.Empty, raisedPropertyName);
        }

        [TestMethod]
        public void SetProperty_WithNullPropertyName_RaisesEventWithNullName()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            var field = 10;
            var eventRaised = false;
            var raisedPropertyName = "NotNull";
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                raisedPropertyName = args.PropertyName;
            };

            // Act
            var result = node.SetPropertyPublic(ref field, 20, null!);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(20, field);
            Assert.IsTrue(eventRaised);
            Assert.IsNull(raisedPropertyName);
        }

        [TestMethod]
        public void OnPropertyChanged_WithPropertyName_RaisesEvent()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            var eventRaised = false;
            string? raisedPropertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                raisedPropertyName = args.PropertyName;
            };

            // Act
            node.OnPropertyChangedPublic("TestProperty");

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual("TestProperty", raisedPropertyName);
        }

        [TestMethod]
        public void OnPropertyChanged_WithNullPropertyName_RaisesEventWithNull()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            var eventRaised = false;
            var raisedPropertyName = "NotNull";
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                raisedPropertyName = args.PropertyName;
            };

            // Act
            node.OnPropertyChangedPublic(null!);

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.IsNull(raisedPropertyName);
        }

        [TestMethod]
        public void OnPropertyChanged_WithEmptyPropertyName_RaisesEventWithEmpty()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            var eventRaised = false;
            string? raisedPropertyName = null;
            node.PropertyChanged += (sender, args) =>
            {
                eventRaised = true;
                raisedPropertyName = args.PropertyName;
            };

            // Act
            node.OnPropertyChangedPublic(string.Empty);

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual(string.Empty, raisedPropertyName);
        }

        [TestMethod]
        public void OnPropertyChanged_WithNoEventHandler_DoesNotThrowException()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");

            // Act & Assert
            node.OnPropertyChangedPublic("TestProperty");
        }

        [TestMethod]
        public void OnPropertyChanged_CalledMultipleTimes_RaisesEventEachTime()
        {
            // Arrange
            var node = new TestableExplorerNode("Label");
            var eventCount = 0;
            node.PropertyChanged += (sender, args) => eventCount++;

            // Act
            node.OnPropertyChangedPublic("Property1");
            node.OnPropertyChangedPublic("Property2");
            node.OnPropertyChangedPublic("Property3");

            // Assert
            Assert.AreEqual(3, eventCount);
        }

        private sealed class TestExplorerNode(string label) : ExplorerNodeBase(label)
        {
            public override ImageMoniker IconMoniker => default;

            public override int ContextMenuId => 0;

            public override bool SupportsChildren => false;
        }

        private sealed class TestExplorerNodeWithLoadTracking(string label) : ExplorerNodeBase(label)
        {
            public int LoadChildrenCallCount { get; private set; }
            public CancellationToken LastCancellationToken { get; private set; }

            public override ImageMoniker IconMoniker => default;

            public override int ContextMenuId => 0;

            public override bool SupportsChildren => true;

            public override Task LoadChildrenAsync(CancellationToken cancellationToken = default)
            {
                LoadChildrenCallCount++;
                LastCancellationToken = cancellationToken;
                return Task.CompletedTask;
            }
        }

        private sealed class TestableExplorerNode(string label) : ExplorerNodeBase(label)
        {
            public override ImageMoniker IconMoniker => default;

            public override int ContextMenuId => 0;

            public override bool SupportsChildren => true;

            public bool BeginLoadingPublic() => BeginLoading();

            public void EndLoadingPublic() => EndLoading();

            public void AddChildPublic(ExplorerNodeBase child) => AddChild(child);

            public bool SetPropertyPublic<T>(ref T field, T value, string? propertyName = null) => SetProperty(ref field, value, propertyName);

            public void OnPropertyChangedPublic(string? propertyName = null) => OnPropertyChanged(propertyName);
        }

        public TestContext TestContext { get; set; }
    }
}
