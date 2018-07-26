using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Org.BouncyCastle.Math;
using Hoard.GameItems;

#if DEBUG
using System.Diagnostics;
#endif

using Nethereum.Web3.Accounts;

namespace Hoard
{
    /// <summary>
    /// Hoard Service entry point.
    /// </summary>
    public class HoardService
    {
        ///// <summary>
        ///// GameAsset by symbol dictionary.
        ///// </summary>
        //public Dictionary<string, GameAsset> GameAssetSymbolDict { get; private set; } = new Dictionary<string, GameAsset>();

        ///// <summary>
        ///// GameAsset by contract address dictionary.
        ///// </summary>
        //public Dictionary<string, GameAsset> GameAssetAddressDict { get; private set; } = new Dictionary<string, GameAsset>();

        /// <summary>
        /// Game backend description with backend connection informations.
        /// </summary>
        public GBDesc GameBackendDesc { get; private set; } = new GBDesc();

        /// <summary>
        /// Game backend connection.
        /// </summary>
        public GBClient GameBackendClient { get; private set; } = null;

        /// <summary>
        /// Game exchange service.
        /// </summary>
        public ExchangeService GameExchangeService { get; private set; } = null;

        /// <summary>
        /// A list of providers for given asset type. Providers are registered using RegisterProvider.
        /// </summary>
        private Dictionary<string, IProvider> Providers = new Dictionary<string, IProvider>();

        /// <summary>
        /// Dafault provider with signin, game backend and exchange support.
        /// </summary>
        private HoardProvider DefaultProvider = null;

        /// <summary>
        /// Accounts per PlayerID
        /// </summary>
        private Dictionary<PlayerID, Account> accounts = new Dictionary<PlayerID, Account>();

        /// <summary>
        /// Hoard service constructor. All initilization is done in Init function.
        /// </summary>
        public HoardService()
        {}
        
        /// <summary>
        /// Retrives game assets using registered Providers. Beware this function is blocking and may take a long time to finish.
        /// Use RefreshGameAssets for async processing.
        /// </summary>
        /// <returns>True if operations succeeded.</returns>
        public bool RefreshGameItemsSync()
        {
            //GameAssetSymbolDict.Clear();
            //GameAssetAddressDict.Clear();

            //foreach (var providers in Providers)
            //    foreach (var provider in providers.Value)
            //    {
            //        List<IGameAssetProvider> assetProviders = provider.GetGameAssetProviders();

            //        foreach (var ap in assetProviders)
            //        {
            //            GameAssetSymbolDict.Add(ap.AssetSymbol, ap);
            //            if (ga.ContractAddress != null)
            //                GameAssetAddressDict.Add(ga.ContractAddress, ga);
            //        }
            //    }

            return true;
        }

        /// <summary>
        /// Retrives game assets using registered Providers.
        /// </summary>
        /// <returns>Async task that retives game assets.</returns>
        public async Task<bool> RefreshGameItems()
        {
            return await Task.Run(() => RefreshGameItemsSync());
        }
       
        /// <summary>
        /// Request game item transfer to player.
        /// </summary>
        /// <param name="recipient">Transfer address.</param>
        /// <param name="item">Game item to be transfered.</param>
        /// <returns>Async task that transfer game item to the other player.</returns>
        public async Task<bool> RequestGameItemTransfer(PlayerID recipient, GameItem item)
        {
            IGameItemProvider gameItemProvider = GetGameItemProvider(item);
            if (gameItemProvider != null)
            {
                return await gameItemProvider.Transfer(recipient.ID, item);
            }

            return false;
        }

        /// <summary>
        /// Check if player is signed in.
        /// </summary>
        /// <param name="id">Player's id to be checked.</param>
        /// <returns>True if given player is signed in.</returns>
        public bool IsSignedIn(PlayerID id)
        {
            return DefaultProvider.IsSignedIn(id);
        }

        /// <summary>
        /// Sign in given player.
        /// </summary>
        /// <param name="id">Player's id to be signed in.</param>
        /// <returns>True if given player has been successfully signed in.</returns>
        public bool SignIn(PlayerID id)
        {
            if (!DefaultProvider.SignIn(id))
                return false;

            GameBackendDesc = DefaultProvider.GetGameBackendDesc();
            GameBackendClient = DefaultProvider.GetGameBackendClient();
            GameExchangeService = DefaultProvider.GetExchangeService();

            return true;
        }

        /// <summary>
        /// Register provider of items and properties.
        /// </summary>
        /// <param name="assetType">Asset type for which this provider will be registered.</param>
        /// <param name="provider">Provider to be registered.</param>
        public void RegisterProvider(string assetType, IProvider provider)
        {
            if (!Providers.ContainsKey(assetType))
            {
                Providers[assetType] = provider;
            }

            if (provider is HoardProvider)
            {
                DefaultProvider = provider as HoardProvider;
            }
        }

        // FIXME: not needed?
        ///// <summary>
        ///// Retrives game asset properties. Beware this function is blocking and may take a long time to finish.
        ///// Use RequestProperties for async processing.
        ///// </summary>
        ///// <param name="item">Game asset for which properties should be retrived.</param>
        ///// <returns>Operation result.</returns>
        //public Result RequestPropertiesSync(GameAsset item)
        //{
        //    List<Provider> providers = null;
        //    if (Providers.TryGetValue(item.AssetType, out providers))
        //    {
        //        foreach (var provider in providers)
        //        {
        //            return provider.GetProperties(item);
        //        }
        //    }

        //    return new Result();
        //}

        ///// <summary>
        ///// Retrives game asset properties.
        ///// </summary>
        ///// <param name="item">Game asset for which properties should be retrived.</param>
        ///// <returns>Async task that retrives game asset properties.</returns>
        //public async Task<Result> RequestProperties(GameAsset item)
        //{
        //    return await Task.Run(() => RequestPropertiesSync(item));
        //}

        ///// <summary>
        ///// Retrives game asset properties of given type. Beware this function is blocking and may take a long time to finish.
        ///// Use RequestProperties for async processing.
        ///// </summary>
        ///// <param name="item">Game asset for which properties should be retrived.</param>
        ///// <param name="name">Property type to be retrived.</param>
        ///// <returns>Operation result.</returns>
        //public Result RequestPropertiesSync(GameAsset item, string name)
        //{
        //    List<Provider> providers = null;
        //    if (Providers.TryGetValue(item.AssetType, out providers))
        //    {
        //        foreach (var provider in providers)
        //        {
        //            if (provider.GetPropertyNames().Contains<string>(name))
        //                return provider.GetProperties(item);
        //        }
        //    }

        //    return new Result();
        //}

        ///// <summary>
        ///// Retrives game asset properties of given type.
        ///// </summary>
        ///// <param name="item">Game asset for which properties should be retrived.</param>
        ///// <param name="name">Property type to be retrived.</param>
        ///// <returns>Async task that retrives game asset properties of given type.</returns>
        //public async Task<Result> RequestProperties(GameAsset item, string name)
        //{
        //    return await Task.Run(() => RequestPropertiesSync(item, name));
        //}

        // PRIVATE SECTION

        /// <summary>
        /// Connects to BC and fills missing options.
        /// </summary>
        /// <param name="options">Hoard service options.</param>
        /// <returns>True if initialization succeeds.</returns>
        public bool Init(HoardServiceOptions options)
        {
            InitAccounts(options.AccountsDir, options.DefaultAccountPass);

            if (DefaultProvider == null)
                return false;

            if (!DefaultProvider.Init())
                return false;

            RefreshGameItems().Wait();

            return true;
        }

        /// <summary>
        /// Shutdown hoard service.
        /// </summary>
        public void Shutdown()
        {
            //GameAssetSymbolDict.Clear();
            //GameAssetAddressDict.Clear();

            Providers.Clear();

            if (DefaultProvider != null)
                DefaultProvider.Shutdown();

            ClearAccounts();

            GameBackendDesc = null;
            GameBackendClient = null;
            GameExchangeService = null;
        }

        /// <summary>
        /// List of player accounts.
        /// </summary>
        public List<Account> Accounts
        {
            get { return accounts.Values.ToList(); }
        }

        /// <summary>
        /// Return account for given player id.
        /// </summary>
        /// <param name="id">Player id for .</param>
        /// <returns>Player account.</returns>
        public Account GetAccount(PlayerID id)
        {
           return accounts[id];
        }

        /// <summary>
        /// Init accounts. 
        /// </summary>
        /// <param name="path">Path to directory with account files.</param>
        /// <param name="password">Account's password.</param>
        private void InitAccounts(string path, string password) 
        {
#if DEBUG
            Debug.WriteLine(String.Format("Initializing account from path: {0}", path), "INFO");
#endif
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            var accountsFiles = ListAccountsUTCFiles(path);

            // if no account in accounts dir create one with default password.
            if (accountsFiles.Length == 0)
            {
#if DEBUG
                Debug.WriteLine("No account found. Generating one.", "INFO");
#endif
                accountsFiles = new string[1];
                accountsFiles[0] = AccountCreator.CreateAccountUTCFile(password, path);
            }

            foreach(var fileName in accountsFiles)
            {
#if DEBUG
                Debug.WriteLine(String.Format("Loading account {0}", fileName), "INFO");
#endif
                var json = File.ReadAllText(System.IO.Path.Combine(path, fileName));

                var account = Account.LoadFromKeyStore(json, password);
                this.accounts.Add(account.Address, account);
            }

#if DEBUG
            Debug.WriteLine("Accounts initialized.", "INFO");
#endif
        }

        /// <summary>
        /// Forget any cached accounts.
        /// </summary>
        private void ClearAccounts()
        {
            accounts.Clear();
        }

        /// <summary>
        /// Return account files from given directory. Account are stored in UTC compliant files.
        /// </summary>
        /// <param name="path">path to account files.</param>
        /// <returns>Account filenames.</returns>
        private string[] ListAccountsUTCFiles(string path)
        {
            return Directory.GetFiles(path, "UTC--*");
        }

        /// <summary>
        /// Returns provider for given game item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private IGameItemProvider GetGameItemProvider(GameItem item)
        {
            IGameItemProvider gameItemProvider = null;
            foreach (var provider in Providers.Values)
            {
                gameItemProvider = provider.GetGameItemProvider(item);
                if (gameItemProvider != null)
                {
                    break;
                }
            }

            return gameItemProvider;
        }
    }
}
