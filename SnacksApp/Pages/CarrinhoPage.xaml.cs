using SnacksApp.Models;
using SnacksApp.Services;
using SnacksApp.Validations;
using System.Collections.ObjectModel;

namespace SnacksApp.Pages;

public partial class CarrinhoPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly IValidator _validator;
    private bool _loginPageDisplayed = false;
    private bool _isNavigatingToEmptyCartPage = false;

    private ObservableCollection<CarrinhoCompraItem>
        ItensCarrinhoCompra = new ObservableCollection<CarrinhoCompraItem>();

    public CarrinhoPage(ApiService apiService, IValidator validator)
    {
        InitializeComponent();
        _apiService = apiService;
        _validator = validator;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (IsNavigatingToEmptyCartPage()) return;

        bool hasItems = await GetItensCarrinhoCompra();

        if (hasItems)
        {
            ExibirEndereco();
        }
        else
        {
            await NavegarParaCarrinhoVazio();
        }
    }

    private bool IsNavigatingToEmptyCartPage()
    {
        if (_isNavigatingToEmptyCartPage)
        {
            _isNavigatingToEmptyCartPage = false;
            return true;
        }
        return false;
    }

    private void ExibirEndereco()
    {
        bool enderecoSalvo = Preferences.ContainsKey("endereco");

        if (enderecoSalvo)
        {
            string nome = Preferences.Get("nome", string.Empty);
            string endereco = Preferences.Get("endereco", string.Empty);
            string telefone = Preferences.Get("telefone", string.Empty);

            // Formatar os dados conforme desejado na label
            LblEndereco.Text = $"{nome}\n{endereco} \n{telefone}";
        }
        else
        {
            LblEndereco.Text = "Informe o seu endere�o";
        }
    }

    private async Task NavegarParaCarrinhoVazio()
    {
        LblEndereco.Text = string.Empty;
        _isNavigatingToEmptyCartPage = true;
        await Navigation.PushAsync(new CarrinhoVazioPage());
    }


    private async Task<bool> GetItensCarrinhoCompra()
    {
        try
        {
            var usuarioId = Preferences.Get("usuarioid", 0);
            var (itensCarrinhoCompra, errorMessage) = await
                     _apiService.GetItensCarrinhoCompra(usuarioId);

            if (errorMessage == "Unauthorized" && !_loginPageDisplayed)
            {
                await DisplayLoginPage();
                return false;
            }

            if (itensCarrinhoCompra == null)
            {
                await DisplayAlert("Erro", errorMessage ?? "N�o foi possivel obter os itens do carrinho de compra.", "OK");
                return false;
            }

            ItensCarrinhoCompra.Clear();

            foreach (var item in itensCarrinhoCompra)
            {
                ItensCarrinhoCompra.Add(item);
            }

            CvCarrinho.ItemsSource = ItensCarrinhoCompra;
            AtualizaPrecoTotal();

            if (!ItensCarrinhoCompra.Any())
            {
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Ocorreu um erro inesperado: {ex.Message}", "OK");
            return false;
        }
    }


    private void AtualizaPrecoTotal()
    {
        try
        {
            var precoTotal = ItensCarrinhoCompra.Sum(item => item.Preco * item.Quantidade);
            LblPrecoTotal.Text = precoTotal.ToString();
        }
        catch (Exception ex)
        {
            DisplayAlert("Erro", $"Ocorreu um erro ao atualizar o pre�o total: {ex.Message}", "OK");
        }
    }

    private async Task DisplayLoginPage()
    {
        _loginPageDisplayed = true;

        await Navigation.PushAsync(new LoginPage(_apiService, _validator));
    }

    private async void BtnDecrementar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is CarrinhoCompraItem itemCarrinho)
        {
            if (itemCarrinho.Quantidade == 1) return;
            else
            {
                itemCarrinho.Quantidade--;
                AtualizaPrecoTotal();
                await _apiService.AtualizaQuantidadeItemCarrinho(itemCarrinho.ProdutoId, "diminuir");
            }
        }
    }


    private async void BtnIncrementar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is CarrinhoCompraItem itemCarrinho)
        {
            itemCarrinho.Quantidade++;
            AtualizaPrecoTotal();
            await _apiService.AtualizaQuantidadeItemCarrinho(itemCarrinho.ProdutoId, "aumentar");
        }
    }


    private async void BtnDeletar_Clicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.BindingContext is CarrinhoCompraItem itemCarrinho)
        {
            bool resposta = await DisplayAlert("Confirma��o",
                          "Tem certeza que deseja excluir este item do carrinho?", "Sim", "N�o");
            if (resposta)
            {
                ItensCarrinhoCompra.Remove(itemCarrinho);
                AtualizaPrecoTotal();
                await _apiService.AtualizaQuantidadeItemCarrinho(itemCarrinho.ProdutoId, "deletar");
            }
        }
    }


    private void BtnEditaEndereco_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new EnderecoPage());
    }

    private async void TapConfirmarPedido_Tapped(object sender, TappedEventArgs e)
    {
        if (ItensCarrinhoCompra == null || !ItensCarrinhoCompra.Any())
        {
            await DisplayAlert("Informa��o", "Seu carrinho est� vazio ou o pedido j� foi confirmado.", "OK");
            return;
        }

        var pedido = new Pedido()
        {
            Endereco = LblEndereco.Text,
            UsuarioId = Preferences.Get("usuarioid", 0),
            ValorTotal = Convert.ToDecimal(LblPrecoTotal.Text)
        };

        var response = await _apiService.ConfirmarPedido(pedido);

        if (response.HasError)
        {
            if (response.ErrorMessage == "Unauthorized")
            {
                // Redirecionar para a p gina de login
                await DisplayLoginPage();
                return;
            }
            await DisplayAlert("Opa !!!", $"Algo deu errado: {response.ErrorMessage}", "Cancelar");
            return;
        }

        ItensCarrinhoCompra.Clear();
        LblEndereco.Text = "Informe o seu endere�o";
        LblPrecoTotal.Text = "0.00";

        await Navigation.PushAsync(new PedidoConfirmadoPage());

    }

}