using System;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.ObjectPool;

namespace Orleans.Runtime
{
    /// <summary>
    /// An <see cref="IBufferWriter{T}"/> that allocates memory from a memory pool.
    /// </summary>
    /// <typeparam name="T">The type of element written by this writer.</typeparam>
    public class OwnedSequence<T> : IBufferWriter<T>, IDisposable
    {
        private const int DefaultBufferSize = 4 * 1024;

        private static readonly ObjectPool<SequenceSegment> SegmentPool = ObjectPool.Create<SequenceSegment>();

        private readonly MemoryPool<T> _memoryPool;

        private SequenceSegment _first;

        private SequenceSegment _last;

        /// <summary>
        /// Initializes a new instance of the <see cref="OwnedSequence{T}"/> class.
        /// </summary>
        /// <param name="memoryPool">The pool to use for recycling backing arrays.</param>
        public OwnedSequence(MemoryPool<T> memoryPool)
        {
            _memoryPool = memoryPool ?? ThrowNull();

            MemoryPool<T> ThrowNull() => throw new ArgumentNullException(nameof(memoryPool));
        }

        /// <summary>
        /// Gets this sequence expressed as a <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        /// <returns>A read only sequence representing the data in this object.</returns>
        public ReadOnlySequence<T> AsReadOnlySequence => this;

        /// <summary>
        /// Gets the length of the sequence.
        /// </summary>
        public long Length => AsReadOnlySequence.Length;

        /// <summary>
        /// Expresses this sequence as a <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        /// <param name="sequence">The sequence to convert.</param>
        public static implicit operator ReadOnlySequence<T>(OwnedSequence<T> sequence)
        {
            return sequence._first != null
                ? new ReadOnlySequence<T>(sequence._first, 0, sequence._last, sequence._last.Length)
                : ReadOnlySequence<T>.Empty;
        }

        /// <summary>
        /// Advances the sequence to include the specified number of elements initialized into memory
        /// returned by a prior call to <see cref="GetMemory(int)"/>.
        /// </summary>
        /// <param name="count">The number of elements written into memory.</param>
        public void Advance(int count)
        {
            if (count < 0) ThrowNegative();
            _last.Length += count;

            void ThrowNegative() => throw new ArgumentOutOfRangeException(
                nameof(count),
                "Value must be greater than or equal to 0");
        }

        /// <summary>
        /// Gets writable memory that can be initialized and added to the sequence via a subsequent call to <see cref="Advance(int)"/>.
        /// </summary>
        /// <param name="sizeHint">The size of the memory required, or 0 to just get a convenient (non-empty) buffer.</param>
        /// <returns>The requested memory.</returns>
        public Memory<T> GetMemory(int sizeHint)
        {
            if (sizeHint < 0) ThrowNegative();

            if (sizeHint == 0)
            {
                if (_last?.WritableBytes > 0)
                {
                    sizeHint = _last.WritableBytes;
                }
                else
                {
                    sizeHint = DefaultBufferSize;
                }
            }

            if (_last == null || _last.WritableBytes < sizeHint)
            {
                Append(_memoryPool.Rent(Math.Min(sizeHint, _memoryPool.MaxBufferSize)));
            }

            return _last.TrailingSlack;

            void ThrowNegative() => throw new ArgumentOutOfRangeException(
               nameof(sizeHint),
               "Value for must be greater than or equal to 0");
        }

        /// <summary>
        /// Gets writable memory that can be initialized and added to the sequence via a subsequent call to <see cref="Advance(int)"/>.
        /// </summary>
        /// <param name="sizeHint">The size of the memory required, or 0 to just get a convenient (non-empty) buffer.</param>
        /// <returns>The requested memory.</returns>
        public Span<T> GetSpan(int sizeHint) => GetMemory(sizeHint).Span;

        /// <summary>
        /// Clears the entire sequence, recycles associated memory into pools,
        /// and resets this instance for reuse.
        /// This invalidates any <see cref="ReadOnlySequence{T}"/> previously produced by this instance.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Dispose() => Reset();

        /// <summary>
        /// Clears the entire sequence and recycles associated memory into pools.
        /// This invalidates any <see cref="ReadOnlySequence{T}"/> previously produced by this instance.
        /// </summary>
        public void Reset()
        {
            var current = _first;
            while (current != null)
            {
                current = RecycleAndGetNext(current);
            }

            _first = _last = null;

            static SequenceSegment RecycleAndGetNext(SequenceSegment segment)
            {
                var recycledSegment = segment;
                segment = segment.Next;
                recycledSegment.ResetMemory();
                SegmentPool.Return(recycledSegment);
                return segment;
            }
        }

        private void Append(IMemoryOwner<T> array)
        {
            if (array == null) ThrowNull();

            var segment = SegmentPool.Get();
            segment.SetMemory(array, 0);

            if (_last == null)
            {
                _first = _last = segment;
            }
            else
            {
                if (_last.Length > 0)
                {
                    // Add a new block.
                    _last.SetNext(segment);
                }
                else
                {
                    // The last block is completely unused. Replace it instead of appending to it.
                    var current = _first;
                    if (_first != _last)
                    {
                        while (current.Next != _last)
                        {
                            current = current.Next;
                        }
                    }
                    else
                    {
                        _first = segment;
                    }

                    current.SetNext(segment);

                    // Return the unused segment to the pool.
                    _last.ResetMemory();
                    SegmentPool.Return(_last);
                }

                _last = segment;
            }

            void ThrowNull() => throw new ArgumentNullException(nameof(array));
        }

        private class SequenceSegment : ReadOnlySequenceSegment<T>
        {
            /// <summary>
            /// Backing field for the <see cref="Length"/> property.
            /// </summary>
            private int _length;

            /// <summary>
            /// Gets or sets the index of the element just beyond the end in <see cref="AvailableMemory"/> to consider part of the sequence.
            /// </summary>
            /// <remarks>
            /// The <see cref="Length"/> represents the offset into <see cref="AvailableMemory"/> where the range of "active" bytes ends. At the point when the block is leased
            /// the <see cref="Length"/> is guaranteed to be equal to 0.
            /// </remarks>
            internal int Length
            {
                get => _length;
                set
                {
                    if (value > AvailableMemory.Length) ThrowOutOfRange();

                    _length = value;

                    Memory = AvailableMemory.Slice(0, value);

                    void ThrowOutOfRange() =>
                        throw new ArgumentOutOfRangeException(nameof(value), "Value must be less than or equal to AvailableMemory.Length");
                }
            }

            internal Memory<T> TrailingSlack => AvailableMemory.Slice(Length);

            internal IMemoryOwner<T> MemoryOwner { get; private set; }

            internal Memory<T> AvailableMemory { get; private set; }

            /// <summary>
            /// Gets the amount of writable bytes in this segment.
            /// It is the amount of bytes between <see cref="OwnedSequence{T}.Length"/> and <see cref="Length"/>.
            /// </summary>
            internal int WritableBytes
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => AvailableMemory.Length - Length;
            }

            internal new SequenceSegment Next
            {
                get => (SequenceSegment)base.Next;
                set => base.Next = value;
            }

            internal void SetMemory(IMemoryOwner<T> memoryOwner, int end)
            {
                MemoryOwner = memoryOwner;
                AvailableMemory = MemoryOwner.Memory;

                RunningIndex = 0;
                Length = end;
                Next = null;
            }

            internal void ResetMemory()
            {
                MemoryOwner.Dispose();
                MemoryOwner = null;
                AvailableMemory = default;

                Memory = default;
                Next = null;
                _length = 0;
            }

            internal void SetNext(SequenceSegment segment)
            {
                if (segment == null) ThrowNull();

                Next = segment;
                segment.RunningIndex = RunningIndex + Length;

                SequenceSegment ThrowNull() => throw new ArgumentNullException(nameof(segment));
            }
        }
    }
}
