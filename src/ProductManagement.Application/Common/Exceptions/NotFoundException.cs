namespace ProductManagement.Application.Common.Exceptions;

public class NotFoundException(string entityName, Guid id)
    : Exception($"{entityName} with id '{id}' was not found.");
