Feature: Currency conversion
  Amounts are converted at the day's reference rate and rounded to the minor unit of the target currency.
  Rates are quoted per euro; a rate between two other currencies goes through the euro.

  Background:
    Given the reference rates of 2026-03-02
      | currency | per euro |
      | USD      | 1.10     |
      | GBP      | 0.85     |
      | JPY      | 160      |

  Scenario: Converting from the base currency
    When 100.00 EUR is converted to USD
    Then the result is 110.00 USD

  Scenario: Cross rates go through the base currency
    When 110.00 USD is converted to GBP
    Then the result is 85.00 GBP

  Scenario: A currency without minor units is rounded to whole units
    When 10.03 EUR is converted to JPY
    Then the result is 1605 JPY

  Scenario: Splitting a bill never loses a cent
    When 100.00 EUR is split into 3 equal parts
    Then the parts are 33.34, 33.33 and 33.33 EUR
