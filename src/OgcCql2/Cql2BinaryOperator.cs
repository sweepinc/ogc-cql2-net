namespace OgcCql2;

/// <summary>
/// Supported binary operators in the CQL2 expression model.
/// </summary>
public enum Cql2BinaryOperator
{
    /// <summary>
    /// Logical conjunction.
    /// </summary>
    And,

    /// <summary>
    /// Logical disjunction.
    /// </summary>
    Or,

    /// <summary>
    /// Equality comparison.
    /// </summary>
    Equal,

    /// <summary>
    /// Inequality comparison.
    /// </summary>
    NotEqual,

    /// <summary>
    /// Less-than comparison.
    /// </summary>
    LessThan,

    /// <summary>
    /// Less-than-or-equal comparison.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Greater-than comparison.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater-than-or-equal comparison.
    /// </summary>
    GreaterThanOrEqual
}
