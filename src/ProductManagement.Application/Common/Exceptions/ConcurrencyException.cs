namespace ProductManagement.Application.Common.Exceptions;

public class ConcurrencyException()
    : Exception("The record was modified by another user. Please refresh and try again.");
