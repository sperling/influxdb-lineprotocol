﻿using InfluxDB.LineProtocol.Collector;
using System;

namespace InfluxDB.LineProtocol
{
    public class CollectorConfiguration
    {
        readonly IPointEmitter _parent;
        readonly PipelinedCollectorTagConfiguration _tag;
        readonly PipelinedCollectorEmitConfiguration _emitter;
        readonly PipelinedCollectorBatchConfiguration _batcher;

        public CollectorConfiguration()
            : this(null)
        {
        }

        internal CollectorConfiguration(IPointEmitter parent = null)
        {
            _parent = parent;
            _tag = new PipelinedCollectorTagConfiguration(this);
            _emitter = new PipelinedCollectorEmitConfiguration(this);
            _batcher = new PipelinedCollectorBatchConfiguration(this);
        }

        public CollectorTagConfiguration Tag
        {
            get { return _tag; }
        }

        public CollectorEmitConfiguration WriteTo
        {
            get { return _emitter; }
        }

        public CollectorBatchConfiguration Batch
        {
            get { return _batcher; }
        }

        public MetricsCollector CreateCollector()
        {
            Action disposeEmitter;
            Action disposeBatcher;

            var emitter = _parent;
            emitter = _emitter.CreateEmitter(emitter, out disposeEmitter);
            emitter = _batcher.CreateEmitter(emitter, out disposeBatcher);

            return new PipelinedMetricsCollector(emitter, _tag.CreateEnricher(), () =>
            {
                if (disposeBatcher != null)
                    disposeBatcher();

                if (disposeEmitter != null)
                    disposeEmitter();
            });
        }
    }
}
