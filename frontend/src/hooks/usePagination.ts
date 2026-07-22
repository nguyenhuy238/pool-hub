import { useState, useCallback } from "react";

export function usePagination<T extends Record<string, any>>(
  initialFilters: T,
  initialPageSize = 10
) {
  const [query, setQuery] = useState({
    ...initialFilters,
    pageNumber: 1,
    pageSize: initialPageSize,
  });

  const changePage = useCallback((pageNumber: number) => {
    setQuery((prev) => ({ ...prev, pageNumber }));
  }, []);

  const changePageSize = useCallback((pageSize: number) => {
    setQuery((prev) => ({ ...prev, pageSize, pageNumber: 1 }));
  }, []);

  const setFilter = useCallback((key: keyof T, value: any) => {
    setQuery((prev) => ({ ...prev, [key]: value, pageNumber: 1 }));
  }, []);

  const updateFilters = useCallback((newFilters: Partial<T>) => {
    setQuery((prev) => ({ ...prev, ...newFilters, pageNumber: 1 }));
  }, []);

  return {
    query,
    setQuery,
    changePage,
    changePageSize,
    setFilter,
    updateFilters,
  };
}
