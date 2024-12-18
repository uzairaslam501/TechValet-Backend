namespace ITValet.Utils.Helpers
{
    public class DatatableHelper<T>
    {
        public List<T> ApplySorting(IEnumerable<T> data, string? sortColumnName, string? sortDirection)
        {
            if (!string.IsNullOrEmpty(sortColumnName) && sortColumnName != "0")
            {
                return sortDirection == "asc"
                    ? data.OrderBy(o => o.GetType().GetProperty(sortColumnName)?.GetValue(o)).ToList()
                    : data.OrderByDescending(o => o.GetType().GetProperty(sortColumnName)?.GetValue(o)).ToList();
            }

            return data.ToList();
        }

        public List<T> ApplyFiltering(IEnumerable<T> data, Func<T, bool> filterPredicate)
        {
            return data.Where(filterPredicate).ToList();
        }

        public List<T> ApplyPagination(IEnumerable<T> data, int start, int length)
        {
            return data.Skip(start * length).Take(length).ToList();
        }
    }
}
