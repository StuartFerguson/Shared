﻿namespace Shared.EventStore.EventStore
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using global::EventStore.Client;
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck" />
    public class EventStoreConnectionStringHealthCheck : IHealthCheck
    {
        #region Fields

        /// <summary>
        /// The event store client settings
        /// </summary>
        private readonly EventStoreClientSettings EventStoreClientSettings;

        /// <summary>
        /// The user credentials
        /// </summary>
        private readonly UserCredentials UserCredentials;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreConnectionStringHealthCheck" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="userCredentials">The user credentials.</param>
        public EventStoreConnectionStringHealthCheck(EventStoreClientSettings settings, UserCredentials userCredentials = null)
        {
            this.EventStoreClientSettings = settings;
            this.UserCredentials = userCredentials;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Runs the health check, returning the status of the component being checked.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that can be used to cancel the health check.</param>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task`1" /> that completes when the health check has finished, yielding the status of the component being checked.
        /// </returns>
        /// <exception cref="Exception">$all stream not found</exception>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
                                                              CancellationToken cancellationToken = default)
        {
            try
            {
                EventStoreClient client = new EventStoreClient(this.EventStoreClientSettings);
                EventStoreClient.ReadStreamResult readResult = client.ReadStreamAsync(Direction.Forwards,
                                                                                      "$users",
                                                                                      StreamPosition.Start,
                                                                                      userCredentials:this.UserCredentials,
                                                                                      resolveLinkTos:true,
                                                                                      cancellationToken:cancellationToken);
                ReadState readState = await readResult.ReadState;
                if (readState == ReadState.StreamNotFound)
                {
                    throw new Exception("$users stream not found");
                }

                return HealthCheckResult.Healthy();
            }
            catch(Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception:ex);
            }
        }

        #endregion

        #region Others

        /// <summary>
        /// The connection name
        /// </summary>
        private const String CONNECTION_NAME = "AspNetCore ConnectionString HealthCheck Connection";

        #endregion
    }
}