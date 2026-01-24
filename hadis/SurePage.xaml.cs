using hadis.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace hadis
{
    public partial class SurePage : ContentPage
    {
        private int _sureNo;
        private double? _pendingScrollPercent = null;
        private bool _scrollRestored = false;
        private CollectionView _collectionView;
        private KuranViewModel _viewModel;

        public SurePage(int sureNo)
        {
            InitializeComponent();
            _sureNo = sureNo;
            _viewModel = new KuranViewModel(sureNo);
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            var percent = Preferences.Default.Get($"KuranScrollPercent_{_sureNo}", 0.0);
            _pendingScrollPercent = percent;
            _scrollRestored = false;
            _collectionView = this.FindByName<CollectionView>("SureCollectionView");
            if (_collectionView != null)
            {
                _collectionView.Scrolled += CollectionView_Scrolled;
            }
            if (_viewModel.Ayahs is INotifyCollectionChanged notify)
            {
                notify.CollectionChanged += Ayahs_CollectionChanged;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_collectionView != null)
            {
                _collectionView.Scrolled -= CollectionView_Scrolled;
            }
            if (_viewModel.Ayahs is INotifyCollectionChanged notify)
            {
                notify.CollectionChanged -= Ayahs_CollectionChanged;
            }
        }

        private void CollectionView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
        {
            if (_collectionView?.ItemsSource is System.Collections.ICollection items && items.Count > 1)
            {
                var percent = (double)e.FirstVisibleItemIndex / (items.Count - 1);
                Preferences.Default.Set($"KuranScrollPercent_{_sureNo}", percent);
                // Son okunan ayet numarasýný kaydet
                int ayetNo = e.FirstVisibleItemIndex + 1; // 1 tabanlý
                Preferences.Default.Set("KuranSonAyetNo", ayetNo);
            }
        }

        private async void Ayahs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_scrollRestored && _pendingScrollPercent.HasValue && _collectionView != null && _collectionView.ItemsSource is System.Collections.ICollection items && items.Count > 0)
            {
                await Task.Delay(30); // UI'nýn yüklenmesini bekle
                int targetIndex = (int)(_pendingScrollPercent.Value * (items.Count - 1));
                if (targetIndex < 0) targetIndex = 0;
                if (targetIndex >= items.Count) targetIndex = items.Count - 1;
                _collectionView.ScrollTo(targetIndex, position: ScrollToPosition.Start, animate: false);
                _scrollRestored = true;
            }
        }
    }
}
