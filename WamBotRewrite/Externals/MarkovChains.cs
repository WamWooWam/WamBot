//
//  This isn't my code!! 
//  Massive thanks to kcartlidge for writing this ages ago, find it here!
//  https://github.com/kcartlidge/CSharp-Generic-Markov-Chains
//

using System;
using System.Collections.Generic;
using System.Text;

// NOTE: Can be significantly speeded by the use of SortedList<T, List<T>> for the Sequence, but would then
// need Weighting and will have issues with comparisons when using reference types as keys.

namespace MarkovChains
{
    /// <summary>Performs Markov chaining to generate new variations based on training data.</summary>
    /// <typeparam name="T">Any type that implements IComparable. For example a string type would provide
    /// Markov sentences, a character type Markov words.</typeparam>
    class Markov<T> where T : IComparable
    {
        #region Private fields
        /// <summary>The trained list of sequences.</summary>
        private List<Sequence<T>> TrainedSequences = new List<Sequence<T>>();
        /// <summary>A random generator for producing the result.</summary>
        private Random r = new Random();
        /// <summary>The StopKey used by this instance</summary>
        private T StopKey = default(T);
        #endregion
        #region Private support
        /// <summary>Holds details of a key and a possible sequence for it</summary>
        /// <typeparam name="T">Any type that implements IComparable. For example a string type would provide
        /// Markov sentences, a character type Markov words.</typeparam>
        private class Sequence<T> where T : IComparable
        {
            /// <summary>The key for this sequence</summary>
            public T Entry { get; set; }
            /// <summary>One possible chain of entries to follow this key</summary>
            public List<T> EntryChain { get; set; }
        }
        #endregion

        /// <summary>Creates a new instance with the given StopKey</summary>
        /// <param name="StopKey">Marks the end of a sequence during training (eg. space character in a
        /// char-based instance, or a slash-n in string-based ones).</param>
        public Markov(T StopKey)
        {
            this.StopKey = StopKey;
        }

        #region Training
        /// <summary>Given the list of entries, this will create a trained set of sequence of the provided Order.</summary>
        /// <param name="entries">Known entries from which the pattern should derive.</param>
        /// <param name="Order">The length of a trained sequence. 1 is pretty random, 5+ pretty close.</param>
        /// <remarks>This is done outside of the constructor in order to allow incremental training over repeated calls.</remarks>
        public void Train(List<T> TrainingEntries, int Order)
        {
            // Ensure there are enough entries to train at least one sequence.
            if (TrainingEntries.Count < Order) throw new InvalidOperationException("Insufficient training entries to form a sequence.");

            // Go through all the training entries one at a time.
            for (int idx = 0; idx < TrainingEntries.Count; idx++)
            {
                // For each, create a new sequence holding the 'Order' number of subsequent entries.
                // Note that this wraps to the start when doing the final 'Order' entries, which may appear
                // to give spurious results. If this is not done, however, there is a chance the start
                // key when generating new sequences will be one of those final entries and if they only
                // appear in the final 'Order' entries then there will be no trained sequences available.
                T key = TrainingEntries[idx];
                if (key.CompareTo(StopKey) != 0)
                {
                    List<T> entryChain = new List<T>();
                    for (int i = 0; i < Order; i++)
                    {
                        T e = TrainingEntries[(idx + i + 1) % TrainingEntries.Count];
                        entryChain.Add(e);
                        if (e.CompareTo(StopKey) == 0) break;
                    }

                    // Add the new trained sequence into the collection.
                    if (entryChain.Count > 0)
                    {
                        Sequence<T> entry = new Sequence<T>();
                        entry.Entry = key;
                        entry.EntryChain = entryChain;
                        TrainedSequences.Add(entry);
                    }
                }
            }
        }
        #endregion
        #region Generation
        /// <summary>Generates a random sequence of around the given length, whose closeness to
        /// the training material will depend upon the Order given in training and the volume
        /// of entries provided.</summary>
        /// <param name="IdealLength">The returned sequence will be at least this length. It may be slightly
        /// longer depending upon how the original Order factors into this length.</param>
        /// <returns>A list of values in sequence.</returns>
        public List<T> Generate(int IdealLength, bool IncludeStopKey)
        {
            List<T> Result = new List<T>();

            // Get a random starting point
            T key = TrainedSequences[r.Next(0, TrainedSequences.Count)].Entry;
            Result.Add(key);

            while (Result.Count < IdealLength)
            {
                // Count the sequences that start at that point, and randomly choose one
                int sequences = 0;
                for (int idx = 0; idx < TrainedSequences.Count; idx++)
                    if (TrainedSequences[idx].Entry.CompareTo(key) == 0) sequences++;

                // Loop through the random sequences that start with the key (if any)
                if (sequences > 0)
                {
                    int wantedSequence = r.Next(0, sequences);
                    sequences = 0;
                    for (int idx = 0; idx < TrainedSequences.Count; idx++)
                        if (TrainedSequences[idx].Entry.CompareTo(key) == 0)
                        {
                            // Check this sequence iteration is the one we want
                            if (sequences == wantedSequence)
                            {
                                // Add the contents of the sequence and set the key ready for the next 'phrase'
                                for (int i = 0; i < TrainedSequences[idx].EntryChain.Count; i++)
                                {
                                    T NewBit = TrainedSequences[idx].EntryChain[i];
                                    // If it's the stop key, only include it if requested initially
                                    if ((NewBit.CompareTo(StopKey) != 0) || IncludeStopKey)
                                    {
                                        Result.Add(NewBit);
                                        key = TrainedSequences[idx].EntryChain[i];
                                    }
                                }
                                break;
                            }
                            sequences++;
                        }
                }
                else
                {
                    // No sequences (may be the key was the last before a stop-key), so randomly start somewhere else
                    key = TrainedSequences[r.Next(0, TrainedSequences.Count)].Entry;
                    Result.Add(key);
                }
            }

            // The ideal length has been reached (or exceeded) so now done
            return Result;
        }
        /// <summary>Generates a random sequence of around the given length, whose closeness to
        /// the training material will depend upon the Order given in training and the volume
        /// of entries provided.</summary>
        /// <param name="IdealLength">The returned sequence will be at least this length. It may be slightly
        /// longer depending upon how the original Order factors into this length.</param>
        /// <param name="Separator">The resulting sequence will be converted into a string, with this value
        /// being inserted between each one. For example, if generating words from a char type then this would
        /// be an empty string whereas if generating phrases from a string type it might be a space.</param>
        /// <returns>A list of values in sequence.</returns>
        public string Generate(int IdealLength, string Separator, bool IncludeStopKey)
        {
            StringBuilder Result = new StringBuilder();
            List<T> Intermediate = Generate(IdealLength, IncludeStopKey);
            for (int idx = 0; idx < Intermediate.Count; idx++)
                Result.Append((idx > 0 ? Separator : "") + Intermediate[idx].ToString());
            return Result.ToString();
        }
        #endregion
    }
}