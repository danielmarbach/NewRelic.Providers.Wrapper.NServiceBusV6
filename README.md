This plugin allows instrumentation of NSB6 proceses using the New Relic .NET Agent. There are two parts to this project: the XML file which simply instructs which methods to instrument and the .dll extension which provides extra information to New Relic so it can understand different message types and provide deeper insight into transactions. 

**To build**
1. Clone the repo
2. Create a lib folder that contains dlls taken from the .NET Agent installation directory, `c:\program files\new relic\.net agent`

  ```
  NewRelic.Agent.Extensions.dll
  NewRelic.SystemExtensions.dll
  ```
3. In the project make sure you have references to those dlls
4. Build the project

**To deploy**

1. Copy the `NewRelic.Providers.Wrapper.NServiceBusV6.Instrumentation.xml` file into the program data folder, typically `C:\ProgramData\New Relic\.NET Agent\Extensions`
2. Copy the newly built binary into the agent extensions directory, typically `c:\program files\new relic\.net agent\extensions`. Also include `NServiceBus.Core.dll`

**Trouble Shooting**

There are a number of places you can look for New Relic configuration issues. The first is in the Windows event log, configuration issues will often appear there. Second is in the log files found in `c:\program files\new relic\.net agent\logs` there are two types of files there the first being the `newrelic_agent` type which will give information about if the process is configured to be monitored. The second is the profiller log whihc will give information about which dlls and methods are being instrumented. 

**Tips**

- If you're using the NServiceBus.Host to run your application then remember to enable new relic in the `NServiceBus.Host.config` file rather than the `yourbinary.dll.config` file

- When setting up for the first time you'll need to restart the process being monitored, but New Relic doesn't need to be restarted

- It may take several minutes for transactions to appear in New Relic

- In the Transactions tab in New Relic you may need to change the drop down from Web to Messages to see the transactions
