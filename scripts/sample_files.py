import os
import random
import shutil

def sample(in_dir, out_dir, n):
    
    if not os.path.exists(out_dir):
        os.makedirs(out_dir)

    sampled_files = random.sample(os.listdir(in_dir), n)
    for file in sampled_files:
        full_path = os.path.join(in_dir, file)
        shutil.copyfile(full_path, os.path.join(out_dir, file))

sample("C:\\Users\\nenad\\Desktop\\Nnd\\doktorske\\training", "C:\\Users\\nenad\\Desktop\\Nnd\\doktorske\\training1000", 1000)
            