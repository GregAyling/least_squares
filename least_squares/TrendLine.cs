using System;
using System.Data;

 public static class TrendLine
{
    public struct LineEquation { public double intercept, slope; };


    /// <summary>
    /// Given a set of points, return the linear equation of best fit.
    /// </summary>
    /// <param name="xy">Set of points.</param>
    /// <returns>Intercept and slope of calcualted best fit line.</returns>
    public static LineEquation LeastSquares(DataTable xy)
        {

            double x, y; // Point co-ordinates.
            int N = 0; // Number of data points.
            double sumx = 0, sumy = 0, sumxy = 0, sumx2 = 0; // Summations.

            foreach (DataRow row in xy.Rows)
            {
                if (row[0] != DBNull.Value && row[1] != DBNull.Value)
                {
                    // Increment count.
                    N++;

                    // Get x,y values.
                    x = Convert.ToDouble(row[0]);
                    y = Convert.ToDouble(row[1]);

                    // Update sums.
                    sumx += x;
                    sumy += y;
                    sumxy += x * y;
                    sumx2 += x * x;
                }
            }

            // Calculate slope and intercept using least squares method.
            double m = ((N * sumxy) - (sumx * sumy)) / ((N * sumx2) - (sumx) * (sumx));
            double b = (sumy - m * sumx) / N;

            // Return line equation.
            return new LineEquation() { intercept = b, slope = m };
        }
    }
