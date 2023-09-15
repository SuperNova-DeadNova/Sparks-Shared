﻿/*
    Ported from Forester.py by dudecon
    Original at http://peripheralarbor.com/minecraft/Forester.py
    From the website: "The scripts are all available to download for free. If you'd like to make your own based on the code, go right ahead."
    
    Copyright 2015 MCGalaxy
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    https://opensource.org/license/ecl-2-0/
    https://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;
using GoldenSparks.Maths;

namespace GoldenSparks.Generator.Foliage {

    /// <summary> Set up the interface for tree objects. Designed for subclassing. </summary>
    public abstract class ForesterTree : Tree {

        public Vec3S32 pos;
        protected double random() { return rnd.NextDouble(); }
        protected const double none = double.MaxValue;
        TreeOutput output;
        
        protected const float FOLIAGE_DENSITY = 1.0f;
        protected const float BRANCH_DENSITY  = 1.0f;
        protected const float TRUNK_THICKNESS = 1.0f;
        protected const float TRUNK_HEIGHT    = 0.7f;
        protected const float EDGE_HEIGHT     =  25f;
        protected const bool  ROOT_BUTTRESSES = true;
        
        public override long EstimateBlocksAffected() { return (long)height * height * height; }
        
        public override void SetData(Random rnd, int value) { this.rnd = rnd; height = value; }

        /// <summary> Outputs the blocks generated by this tree at the given coordinates. </summary>
        public override void Generate(ushort x, ushort y, ushort z, TreeOutput output) {
            pos.X = x; pos.Y = y; pos.Z = z;
            this.output = output;
            
            Prepare();
            MakeFoliage();
            MakeTrunk();
        }
        
        /// <summary> initialize the internal values for the Tree object. </summary>
        public virtual void Prepare() { }
        
        /// <summary> Generates the trunk </summary>
        public virtual void MakeTrunk() { }

        /// <summary> Generates the foliage </summary>
        public virtual void MakeFoliage() { }

        protected void Place(int x, int y, int z, byte block) {
            if (x < 0 || y < 0 || z < 0) return;
            output((ushort)x, (ushort)y, (ushort)z, block);
        }
    }
    

    /// <summary> Set up the methods for a larger more complicated tree. </summary>
    /// <remarks> This tree type has roots, a trunk, and branches all of varying width,
    /// and many foliage clusters.
    /// MUST BE SUBCLASSED. Specifically, self.foliage_shape must be set.
    /// Subclass 'prepare' and 'shapefunc' to make different shaped trees.</remarks>
    public abstract class ProceduralTree : ForesterTree {
        
        protected List<Vec3S32> foliage_coords = new List<Vec3S32>();
        protected float[] foliage_shape;
        protected float branchslope, branchdensity, trunkradius, trunkheight;

        /// <summary> Create a round section of type matidx in mcmap. </summary>
        /// <remarks> Passed values:
        /// center = [x,y,z] for the coordinates of the center block
        /// radius = {number} as the radius of the section. May be a float or int.
        /// diraxis: The list index for the axis to make the section
        /// perpendicular to. 0 indicates the x axis, 1 the y, 2 the z.  The
        /// section will extend along the other two axies.
        /// </remarks>
        protected void CrossSection(Vec3S32 center, float radius, int diraxis, byte block) {
            int rad = (int)(radius + 0.618f);
            if (rad <= 0) return;
            
            int secidx1 = (diraxis - 1) % 3;
            int secidx2 = (diraxis + 1) % 3;
            Vec3S32 coord = new Vec3S32(0, 0, 0);
            
            for (int off1 = -rad; off1 <= rad; off1++)
                for (int off2 = -rad; off2 <= rad; off2++)
            {
                float dOff1 = Math.Abs(off1) + 0.5f, dOff2 = Math.Abs(off2) + 0.5f;
                float thisdist = (float)Math.Sqrt(dOff1 * dOff1 + dOff2 * dOff2);
                if (thisdist > radius) continue;
                
                coord[diraxis] = center[diraxis];
                coord[secidx1] = center[secidx1] + off1;
                coord[secidx2] = center[secidx2] + off2;
                Place(coord.X, coord.Y, coord.Z, block);
            }
        }
        
        /// <summary> Take y and return a radius for the location of the foliage cluster. </summary>
        /// <remarks> If no foliage cluster is to be created, return None
        /// Designed for sublcassing.  Only makes clusters close to the trunk. </remarks>
        protected virtual double ShapeFunc(int y) {
            if (random() < (100.0 / (height * height)) && y < trunkheight)
                return height * 0.12;
            return none;
        }

        /// <summary> generate a round cluster of foliage at the location center. </summary>
        /// <remarks> The shape of the cluster is defined by the list self.foliage_shape.
        /// This list must be set in a subclass of ProceduralTree. </remarks>
        protected void FoilageCluster(Vec3S32 center) {
            float[] level_radius = foliage_shape;
            foreach (float i in level_radius) {
                CrossSection(center, i, 1, Block.Leaves);
                center.Y += 1;
            }
        }

        /// <summary> Create a tapered cylinder in mcmap. </summary>
        /// <remarks> Passed values:
        /// start and end are the beginning and ending coordinates of form [x,y,z].
        /// startsize and endsize are the beginning and ending radius.
        /// </remarks>
        protected void TaperedCylinder(Vec3S32 start, Vec3S32 end, float startsize, float endsize) {
            // delta is the coordinate vector for the difference between start and end.
            Vec3S32 delta = end - start;
            
            // primidx is the index (0,1,or 2 for x,y,z) for the coordinate
            // which has the largest overall delta.
            int primidx = 0;
            float maxdist = Math.Abs(delta.X);
            if (Math.Abs(delta.Y) > maxdist) { primidx = 1; maxdist = Math.Abs(delta.Y); }
            if (Math.Abs(delta.Z) > maxdist) { primidx = 2; maxdist = Math.Abs(delta.Z); }
            if (maxdist == 0) return;
            
            // secidx1 and secidx2 are the remaining indicies out of [0,1,2].
            int secidx1 = (primidx - 1) % 3;
            int secidx2 = (primidx + 1) % 3;
            
            // primsign is the digit 1 or -1 depending on whether the limb is headed
            // along the positive or negative primidx axis.
            int primsign = Math.Sign(delta[primidx]);
            
            // secdelta1 and ...2 are the amount the associated values change
            // for every step along the prime axis.
            int secdelta1 = delta[secidx1];
            float secfac1 = (float)secdelta1 / delta[primidx];
            int secdelta2 = delta[secidx2];
            float secfac2 = (float)secdelta2 / delta[primidx];
            
            // Initialize coord. These values could be anything, since they are overwritten.
            Vec3S32 coord = new Vec3S32(0, 0, 0);
            // Loop through each crossection along the primary axis, from start to end.
            int endoffset = delta[primidx] + primsign;
            
            for (int primoffset = 0; primoffset != endoffset; primoffset += primsign) {
                int primloc = start[primidx] + primoffset;
                int secloc1 = (int)(start[secidx1] + primoffset * secfac1);
                int secloc2 = (int)(start[secidx2] + primoffset * secfac2);
                
                coord[primidx] = primloc;
                coord[secidx1] = secloc1;
                coord[secidx2] = secloc2;
                
                float primdist = Math.Abs(delta[primidx]);
                float radius = endsize + (startsize - endsize) * Math.Abs(delta[primidx] - primoffset) / primdist;
                CrossSection(coord, radius, primidx, Block.Log);
            }
        }

        /// <summary> Generates the foliage for the tree </summary>
        public override void MakeFoliage() {
            foreach (Vec3S32 coord in foliage_coords) {
                FoilageCluster(coord);
            }
            foreach (Vec3S32 coord in foliage_coords) {
                Place(coord.X, coord.Y, coord.Z, Block.Leaves);
            }
        }

        /// <summary> Generates the branches </summary>
        protected void MakeBranches() {
            Vec3S32 treeposition = pos;
            int topy = treeposition.Y + (int)(trunkheight + 0.5f);
            // endrad is the base radius of the branches at the trunk
            float endrad = trunkradius * (1 - trunkheight / height);
            if (endrad < 1) endrad = 1;
            
            foreach (Vec3S32 coord in foliage_coords) {
                Vec3S32 delta = coord - treeposition;
                float dist = (float)Math.Sqrt(delta.X * delta.X + delta.Z * delta.Z);
                float ydist = coord.Y - treeposition.Y;
                
                // value is a magic number that weights the probability
                // of generating branches properly so that
                // you get enough on small trees, but not too many on larger trees.
                // Very difficult to get right... do not touch!
                double value = (branchdensity * 220 * height) / (Math.Pow(ydist + dist, 3));
                if (value < random()) continue;
                
                float slope = (float)(branchslope + (0.5 - random()) * 0.16);
                float branchy, basesize;
                if ((coord.Y - dist * slope) > topy) {
                    // Another random rejection, for branches between
                    // the top of the trunk and the crown of the tree
                    float threshhold = 1.0f / height;
                    if (random() < threshhold) continue;
                    
                    branchy = topy;
                    basesize = endrad;
                } else {
                    branchy = coord.Y - dist * slope;
                    basesize = (endrad + (trunkradius - endrad) * 
                                (topy - branchy) / trunkheight);
                }
                
                double startsize = basesize * (1 + random()) * 0.618 * Math.Pow(dist / height, 0.618);
                double rndr = Math.Sqrt(random()) * basesize * 0.618;
                double rndang = random() * 2 * Math.PI;
                
                int rndx = (int)(rndr * Math.Sin(rndang) + 0.5);
                int rndz = (int)(rndr * Math.Cos(rndang) + 0.5);
                Vec3S32 startcoord = new Vec3S32(treeposition.X + rndx, (int)branchy, treeposition.Z + rndz);
                
                if (startsize < 1) startsize = 1;
                float endsize = 1.0f;
                TaperedCylinder(startcoord, coord, (float)startsize, endsize);
            }
        }

        /// <summary> Generates the trunk, roots, and branches </summary>
        public override void MakeTrunk() {
            Vec3S32 treeposition = pos;
            int starty = treeposition.Y;
            int midy   = treeposition.Y + (int)(trunkheight * 0.382f);
            int topy   = treeposition.Y + (int)(trunkheight + 0.5f);
            // In this method, x and z are the position of the trunk.
            int x = treeposition.X, z = treeposition.Z;
            
            float end_size_factor = trunkheight / height;
            float startrad = 0;
            float midrad = trunkradius * (1 - end_size_factor * 0.5f);
            float endrad = trunkradius * (1 - end_size_factor);
            if (endrad < 1) endrad = 1;
            if (midrad < endrad) midrad = endrad;
            
            bool mangrove = (this is MangroveTree);
            // Make the root buttresses, if indicated
            if (ROOT_BUTTRESSES || mangrove) {
                // The start radius of the trunk should be a little smaller if we are using root buttresses.
                startrad = trunkradius * 0.8f;
                float buttress_radius = trunkradius * 0.382f;
                // posradius is how far the root buttresses should be offset from the trunk.
                float posradius = trunkradius;
                // In mangroves, the root buttresses are much more extended.
                if (mangrove) posradius = posradius * 2.618f;
                int num_of_buttresses = (int)(Math.Sqrt(trunkradius) + 3.5);
                
                for (int i = 0; i < num_of_buttresses; i++) {
                    double rndang = random()* 2 * Math.PI;
                    double thisposradius = posradius * (0.9 + random() * 0.2);
                    // thisx and thisz are the x and z position for the base of the root buttress.
                    int thisx = x + (int)(thisposradius * Math.Sin(rndang));
                    int thisz = z + (int)(thisposradius * Math.Cos(rndang));
                    
                    // thisbuttressradius is the radius of the buttress.
                    // Currently, root buttresses do not taper.
                    float thisbuttressradius = buttress_radius * (float)(0.618 + random());
                    if (thisbuttressradius < 1) thisbuttressradius = 1;
                    
                    // Make the root buttress.
                    TaperedCylinder(new Vec3S32(thisx, starty, thisz), new Vec3S32(x, midy, z),
                                    thisbuttressradius, thisbuttressradius);
                }
            } else {
                // If root buttresses are turned off, set the trunk radius to normal size.
                startrad = trunkradius;
            }
            
            // Make the lower and upper sections of the trunk.
            TaperedCylinder(new Vec3S32(x, starty, z), new Vec3S32(x, midy, z), startrad, midrad);
            TaperedCylinder(new Vec3S32(x, midy, z),   new Vec3S32(x, topy, z), midrad,   endrad);
            // Make the branches
            MakeBranches();
        }

        /// <summary> Initialize the internal values for the Tree object. </summary>
        /// <remarks> Primarily, sets up the foliage cluster locations. </remarks>
        public override void Prepare() {
            Vec3S32 treeposition = pos;
            trunkradius = (float)(Math.Sqrt(height * TRUNK_THICKNESS));
            if (trunkradius < 1) trunkradius = 1;
            trunkheight = 0.618f * height;
            int ystart = treeposition.Y, yend = treeposition.Y + height;
            
            branchdensity = BRANCH_DENSITY / FOLIAGE_DENSITY;
            int topy = treeposition.Y + (int)(trunkheight + 0.5);
            int num_of_clusters_per_y = (int)(1.5 + Math.Pow(FOLIAGE_DENSITY * height / 19.0, 2));
            if (num_of_clusters_per_y < 1) num_of_clusters_per_y = 1;
            
            // TODO: fCraft is yEnd - 1, ????
            // Outdated, since maps can be twice as tall now.
            foliage_coords.Clear();
            for (int y = yend; y != ystart; y--)
                for (int i = 0; i < num_of_clusters_per_y; i++)
            {
                double shapefac = ShapeFunc(y - ystart);
                if (shapefac == none) continue;
                double r = (Math.Sqrt(random()) + 0.328) * shapefac;
                
                double theta = random() * 2 * Math.PI;
                int x = (int)(r * Math.Sin(theta)) + treeposition.X;
                int z = (int)(r * Math.Cos(theta)) + treeposition.Z;
                foliage_coords.Add(new Vec3S32(x, y, z));
            }
        }
    }


    /// <summary> This kind of tree is designed to resemble a deciduous tree. </summary>
    public class RoundTree : ProceduralTree {
        
        public override int DefaultSize(Random rnd) { return rnd.Next(6, 11); }
        
        public override void Prepare() {
            base.Prepare();
            branchslope = 0.382f;
            foliage_shape = new float[] { 2, 3, 3, 2.5f, 1.6f };
            trunkradius = trunkradius * 0.8f;
            trunkheight = TRUNK_HEIGHT * trunkheight;
        }
        
        protected override double ShapeFunc(int y) {
            double twigs = base.ShapeFunc(y);
            if (twigs != none) return twigs;
            if (y < height * (0.282 + 0.1 * Math.Sqrt(random())))
                return none;
            
            double radius = height / 2.0;
            double adj    = height / 2.0 - y;
            double dist   = 0;
            
            if (adj == 0) {
                dist = radius;
            } else if (Math.Abs(adj) >= radius) {
                dist = 0;
            } else {
                dist = Math.Sqrt(radius * radius - adj * adj);
            }
            return dist * 0.618;
        }
    }
    
    /// <summary> This kind of tree is designed to resemble a conifer tree. </summary>
    public class ConeTree : ProceduralTree {

        public override int DefaultSize(Random rnd) { return rnd.Next(15, 31); }
        
        public override void Prepare() {
            base.Prepare();
            branchslope = 0.15f;
            foliage_shape = new float[] { 3, 2.6f, 2, 1 };
            trunkradius = trunkradius * 0.5f;
        }
        
        protected override double ShapeFunc(int y) {
            double twigs = base.ShapeFunc(y);
            if (twigs != none) return twigs;
            if (y < height * (0.25 + 0.05 * Math.Sqrt(random())))
                return none;
            
            double radius = (height - y) * 0.382;
            if (radius < 0) radius = 0;
            return radius;
        }
    }

    /// <summary> This kind of tree is designed to resemble a rainforest tree. </summary>
    public class RainforestTree : ProceduralTree {

        public override int DefaultSize(Random rnd) { return rnd.Next(6, 11); }
        
        public override void Prepare() {
            base.Prepare();
            foliage_shape = new float[] { 3.4f, 2.6f };
            branchslope = 1.0f;
            trunkradius = trunkradius * 0.382f;
            trunkheight = trunkheight * 0.9f;
        }
        
        protected override double ShapeFunc(int y) {
            if (y < height * 0.8) {
                if (EDGE_HEIGHT < height) {
                    double twigs = base.ShapeFunc(y);
                    if (twigs != none && random() < 0.07f)
                        return twigs;
                }
                return none;
            } else {
                double width = height * 0.382;
                double topdist = (height - y) / (height * 0.2f);
                double dist = width * (0.618f + topdist) * (0.618f + random()) * 0.382f;
                return dist;
            }
        }
    }
    

    /// <summary> This kind of tree is designed to resemble a mangrove tree. </summary>
    public class MangroveTree : RoundTree {

        public override int DefaultSize(Random rnd) { return rnd.Next(10, 21); }
        
        public override void Prepare() {
            base.Prepare();
            branchslope = 1.0f;
            trunkradius = trunkradius * 0.618f;
        }
        
        protected override double ShapeFunc(int y) {
            double val = base.ShapeFunc(y);
            if (val == none) return none;
            return val * 1.618;
        }
    }
    
    
    public sealed class BambooTree : Tree {
        
        public override long EstimateBlocksAffected() { return height * 2; }
        
        public override int DefaultSize(Random rnd) { return rnd.Next(4, 8); }
        
        public override void SetData(Random rnd, int value) {
            height = value;
            size = 1;
            this.rnd = rnd;
        }
        
        public override void Generate(ushort x, ushort y, ushort z, TreeOutput output) {
            for (int dy = 0; dy <= height; dy++) {
                ushort yy = (ushort)(y + dy);
                if (dy < height) output(x, yy, z, Block.Log);
                
                for (int i = 0; i < 2; i++) {
                    int dx = rnd.NextDouble() >= 0.5 ? 1 : -1;
                    int dz = rnd.NextDouble() >= 0.5 ? 1 : -1;
                    output((ushort)(x + dx), yy, (ushort)(z + dz), Block.Leaves);
                }
            }
        }
    }
    
    public sealed class PalmTree : Tree {
        
        public override long EstimateBlocksAffected() { return height + 8; }
        
        public override int DefaultSize(Random rnd) { return rnd.Next(4, 8); }

        public override void SetData(Random rnd, int value) {
            height = value;
            size = 2;
            this.rnd = rnd;
        }
        
        public override void Generate(ushort x, ushort y, ushort z, TreeOutput output) {
            for (int dy = 0; dy <= height; dy++)
                if (dy < height) output(x, (ushort)(y + dy), z, Block.Log);
            
            for (int dz = -2; dz <= 2; dz++)
                for (int dx = -2; dx <= 2; dx++)
            {
                if (Math.Abs(dx) != Math.Abs(dz)) continue;
                output((ushort)(x + dx), (ushort)(y + height), (ushort)(z + dz), Block.Leaves);
            }
        }
    }
}
