using System.CommandLine;
using Trdr.Recorder;

var applicationContext = new ApplicationContext();

var rootCmd = new RootCommand();
var logOption = new Option<string>("--log") { Arity = ArgumentArity.ExactlyOne };
rootCmd.AddGlobalOption(logOption);
var binanceHandler = new BinanceHandler(applicationContext, logOption);
var coinJarHandler = new CoinJarHandler(applicationContext, logOption);
rootCmd.Add(binanceHandler.GetCommand());
rootCmd.Add(coinJarHandler.GetCommand());

rootCmd.Invoke(args);
return applicationContext.ReturnCode;