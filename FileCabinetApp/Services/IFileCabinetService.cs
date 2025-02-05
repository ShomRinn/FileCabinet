using System;
using System.Collections.ObjectModel;
using FileCabinetApp.Models;

namespace FileCabinetApp.Services
{
    /// <summary>
    /// Provides an interface for file cabinet services.
    /// </summary>
    public interface IFileCabinetService
    {
        /// <summary>
        /// Creates a new record and returns its ID.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <param name="dateOfBirth">The date of birth.</param>
        /// <param name="height">The height.</param>
        /// <param name="salary">The salary.</param>
        /// <param name="gender">The gender.</param>
        /// <returns>The ID of the created record.</returns>
        int CreateRecord(
            string firstName,
            string lastName,
            DateTime dateOfBirth,
            short height,
            decimal salary,
            char gender);

        /// <summary>
        /// Edits an existing record by ID.
        /// </summary>
        /// <param name="id">The ID of the record.</param>
        /// <param name="firstName">The new first name.</param>
        /// <param name="lastName">The new last name.</param>
        /// <param name="dateOfBirth">The new date of birth.</param>
        /// <param name="height">The new height.</param>
        /// <param name="salary">The new salary.</param>
        /// <param name="gender">The new gender.</param>
        void EditRecord(
            int id,
            string firstName,
            string lastName,
            DateTime dateOfBirth,
            short height,
            decimal salary,
            char gender);

        /// <summary>
        /// Returns all active records.
        /// </summary>
        /// <returns>A read-only collection of records.</returns>
        ReadOnlyCollection<FileCabinetRecord> GetRecords();

        /// <summary>
        /// Returns the total count of active (non-deleted) records.
        /// </summary>
        /// <returns>The number of active records.</returns>
        int GetStat();

        /// <summary>
        /// Finds records by first name.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <returns>A read-only collection of matching records.</returns>
        ReadOnlyCollection<FileCabinetRecord> FindByFirstName(string firstName);

        /// <summary>
        /// Finds records by last name.
        /// </summary>
        /// <param name="lastName">The last name.</param>
        /// <returns>A read-only collection of matching records.</returns>
        ReadOnlyCollection<FileCabinetRecord> FindByLastName(string lastName);

        /// <summary>
        /// Finds records by date of birth.
        /// </summary>
        /// <param name="dateOfBirth">The date of birth.</param>
        /// <returns>A read-only collection of matching records.</returns>
        ReadOnlyCollection<FileCabinetRecord> FindByDateOfBirth(DateTime dateOfBirth);

        /// <summary>
        /// Creates a snapshot of the current records.
        /// </summary>
        /// <returns>The snapshot object.</returns>
        FileCabinetServiceSnapshot MakeSnapshot();

        /// <summary>
        /// Restores data from the specified snapshot.
        /// Records that already exist (matching ID) get updated, new ones get added.
        /// Invalid records are skipped with an error message.
        /// </summary>
        /// <param name="snapshot">The snapshot from which to restore.</param>
        void Restore(FileCabinetServiceSnapshot snapshot);

        /// <summary>
        /// Removes a record by ID. 
        /// For memory-based service, the record is removed from the collection.
        /// For file-based service, the record is marked as deleted.
        /// </summary>
        /// <param name="id">The record ID to remove.</param>
        /// <returns>True if a record was removed (or marked as deleted), false if not found.</returns>
        bool RemoveRecord(int id);

        /// <summary>
        /// Returns the number of records marked as deleted (if applicable).
        /// Memory-based service always returns 0, because records are physically removed.
        /// </summary>
        /// <returns>The number of deleted records.</returns>
        int GetDeletedCount();
    }
}