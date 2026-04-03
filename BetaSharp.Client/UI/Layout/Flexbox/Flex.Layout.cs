namespace BetaSharp.Client.UI.Layout.Flexbox;

public partial class Flex
{
    internal class CachedMeasurement
    {
        internal float availableWidth;
        internal float availableHeight;
        internal MeasureMode widthMeasureMode = MeasureMode.Undefined;
        internal MeasureMode heightMeasureMode = MeasureMode.Undefined;
        internal float computedWidth = -1;
        internal float computedHeight = -1;

        internal void ResetToDefault()
        {
            this.availableHeight = 0;
            this.availableWidth = 0;
            this.widthMeasureMode = MeasureMode.Undefined;
            this.heightMeasureMode = MeasureMode.Undefined;
            this.computedWidth = -1;
            this.computedHeight = -1;
        }
    }

    internal class Layout
    {

        internal readonly float[] Position = new float[4];
        internal readonly float[] Dimensions = new float[2] { float.NaN, float.NaN };
        internal readonly float[] Margin = new float[6];
        internal readonly float[] Border = new float[6];
        internal readonly float[] Padding = new float[6];
        internal Direction Direction;
        internal int computedFlexBasisGeneration;
        internal float computedFlexBasis = float.NaN;
        internal bool HadOverflow = false;

        // Instead of recomputing the entire layout every single time, we
        // cache some information to break early when nothing changed
        internal int generationCount;
        internal Direction lastParentDirection = Direction.NeverUsed_1;
        internal int nextCachedMeasurementsIndex = 0;
        internal readonly CachedMeasurement[] cachedMeasurements = new CachedMeasurement[Constant.MaxCachedResultCount]
        {
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
            new CachedMeasurement(),
        };
        internal readonly float[] measuredDimensions = new float[2] { float.NaN, float.NaN };
        readonly internal CachedMeasurement cachedLayout = new CachedMeasurement();

        internal void ResetToDefault()
        {
            for (int i = 0; i < this.Position.Length; i++)
            {
                this.Position[i] = 0;
            }
            for (int i = 0; i < Dimensions.Length; i++)
            {
                this.Dimensions[i] = float.NaN;
            }
            for (int i = 0; i < 6; i++)
            {
                this.Margin[i] = 0;
                this.Border[i] = 0;
                this.Padding[i] = 0;
            }
            this.Direction = Direction.Inherit;
            this.computedFlexBasisGeneration = 0;
            this.computedFlexBasis = float.NaN;
            this.HadOverflow = false;
            this.generationCount = 0;
            this.lastParentDirection = Direction.NeverUsed_1;
            this.nextCachedMeasurementsIndex = 0;

            foreach (var cm in this.cachedMeasurements)
            {
                cm.ResetToDefault();
            }

            for (int i = 0; i < measuredDimensions.Length; i++)
            {
                this.measuredDimensions[i] = float.NaN;
            }

            cachedLayout.ResetToDefault();
        }
    }
}
