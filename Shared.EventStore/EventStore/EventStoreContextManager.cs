﻿namespace Shared.EventStore.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using DomainDrivenDesign.EventStore;
    using Microsoft.Extensions.Logging;
    using Repositories;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Shared.EventStore.EventStore.IEventStoreContextManager" />
    public class EventStoreContextManager : IEventStoreContextManager
    {
        #region Fields

        /// <summary>
        /// The connection string configuration repository
        /// </summary>
        private readonly IConnectionStringConfigurationRepository ConnectionStringConfigurationRepository;

        /// <summary>
        /// The context
        /// </summary>
        private readonly IEventStoreContext Context;

        /// <summary>
        /// The event store context function
        /// </summary>
        private readonly Func<String, IEventStoreContext> EventStoreContextFunc;

        /// <summary>
        /// The event store contexts
        /// </summary>
        private readonly Dictionary<String, IEventStoreContext> EventStoreContexts;

        //TODO static?
        /// <summary>
        /// The padlock
        /// </summary>
        private readonly Object padlock = new Object();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreContextManager" /> class.
        /// </summary>
        /// <param name="eventStoreContextFunc">The event store context function.</param>
        /// <param name="connectionStringConfigurationRepository">The connection string configuration repository.</param>
        public EventStoreContextManager(Func<String, IEventStoreContext> eventStoreContextFunc,
                                        IConnectionStringConfigurationRepository connectionStringConfigurationRepository)
        {
            this.EventStoreContexts = new Dictionary<String, IEventStoreContext>();
            this.EventStoreContextFunc = eventStoreContextFunc;
            this.ConnectionStringConfigurationRepository = connectionStringConfigurationRepository;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreContextManager" /> class.
        /// </summary>
        /// <param name="eventStoreContext">The event store context.</param>
        public EventStoreContextManager(IEventStoreContext eventStoreContext)
        {
            this.Context = eventStoreContext;
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [trace generated].
        /// </summary>
        public event TraceHandler TraceGenerated;

        #endregion

        #region Methods

        /// <summary>
        /// Gets the event store context.
        /// </summary>
        /// <param name="connectionIdentifier">The connection identifier.</param>
        /// <returns></returns>
        public IEventStoreContext GetEventStoreContext(String connectionIdentifier)
        {
            if (this.Context != null)
            {
                return this.Context;
            }

            this.WriteTrace($"No resolved context found, about to resolve one using connectionIdentifier {connectionIdentifier}");

            if (this.EventStoreContexts.ContainsKey(connectionIdentifier))
            {
                return this.EventStoreContexts[connectionIdentifier];
            }

            this.WriteTrace($"Creating a new EventStoreContext for connectionIdentifier {connectionIdentifier}");

            lock(this.padlock)
            {
                if (!this.EventStoreContexts.ContainsKey(connectionIdentifier))
                {
                    // This will need to now look up the ES Connection string from persistence
                    String connectionString = this.ConnectionStringConfigurationRepository
                                                  .GetConnectionString(connectionIdentifier, ConnectionStringType.EventStore, CancellationToken.None).Result;

                    this.WriteTrace($"Connection String is {connectionString}");

                    IEventStoreContext eventStoreContext = this.EventStoreContextFunc(connectionString);

                    this.EventStoreContexts.Add(connectionIdentifier, eventStoreContext);
                }

                return this.EventStoreContexts[connectionIdentifier];
            }
        }

        /// <summary>
        /// Guards the against no connection identifier.
        /// </summary>
        /// <param name="connectionIdentifier">The connection identifier.</param>
        /// <exception cref="ArgumentException">Value cannot be empty. - connectionIdentifier</exception>
        private void GuardAgainstNoConnectionIdentifier(String connectionIdentifier)
        {
            //Check if the connectionStringIdentifier is present
            if (string.IsNullOrEmpty(connectionIdentifier))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(connectionIdentifier));
            }
        }

        /// <summary>
        /// Writes the trace.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void WriteTrace(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Information);
            }
        }

        #endregion
    }
}